﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LsMsgPack;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.ComponentModel;
using System.IO;

namespace MsgPackExplorer {
  public partial class LsMsgPackExplorer: UserControl {
    public LsMsgPackExplorer() {
      InitializeComponent();
    }

    private MsgPackItem item;
    [Category("MsgPack")]
    [DisplayName("Item")]
    [Description("The root element of a MsgPack message. If you have the choice rather populate Data so the original stream can be displayed in the hex editor.")]
    public MsgPackItem Item {
      get { return item; }
      set {
        item = value;
        RefreshTree();
      }
    }

    private bool _continueOnError;
    [Category("MsgPack")]
    [DisplayName("Continue On Error")]
    [Description("Set this to true in order to keep processing the stream after a breaking error occurred.")]
    public bool ContinueOnError {
      get { return _continueOnError; }
      set {
        if(value != _continueOnError) {
          _continueOnError = value;
          if(!ReferenceEquals(data, null) && data.Length > 0) Data = data;
        }
      }
    }
    
    private bool _readUntilEof;
    [Category("MsgPack")]
    [DisplayName("Read Until End Of File")]
    [Description("Set this to true in order to keep processing the stream until the end of the file.")]
    public bool ReadUntilEof {
      get { return _readUntilEof; }
      set {
        if(value != _readUntilEof) {
            _readUntilEof = value;
          if(!ReferenceEquals(data, null) && data.Length > 0) Data = data;
        }
      }
    }

    private byte[] data;
    [Category("MsgPack")]
    [DisplayName("Data")]
    [Description("The raw original bytes of a MsgPack message.")]
    public byte[] Data {
      get { return data; }
      set {
        data = value;
        if(ReferenceEquals(value, null)) Item = null;
        else Item = MsgPackItem.Unpack(data, false, true, _continueOnError, _readUntilEof);
      }
    }

    /// <summary>
    /// Clears all the data and starts with an empty slate
    /// </summary>
    public void Clear() {
      Data = null;
    }

    public Image GetIcon() {
      using(var stream = typeof(LsMsgPackExplorer).Assembly.GetManifestResourceStream("Explore")) {
        if(stream != null) {
          return Image.FromStream(stream);
        }
      }
      return null;
    }

    List<EditorMetaData> lineairList = new List<EditorMetaData>();

    private class EditorMetaData {
      public int CharOffset = 0;
      public int Length = 0;
      public TreeNode Node;
      public MsgPackItem Item;
    }

    private void RefreshTree() {
      treeView1.Nodes.Clear();
      richTextBox1.Clear();
      lineairList.Clear();
      listView1.Items.Clear();
      if(ReferenceEquals(item, null)) return;

      TreeNode root = GetTreeNodeFor(item);
      Traverse(root, item);
      treeView1.Nodes.Add(root);
      treeView1.ExpandAll();
      if(ReferenceEquals(data, null) || data.Length == 0) data = item.ToBytes();
      //richTextBox1.Text = BitConverter.ToString(data).Replace('-', ' ');

      string[] hex = BitConverter.ToString(data).Split('-');
      StringBuilder sb = new StringBuilder("{\\rtf1 {\\colortbl ;\\red255\\green0\\blue0;\\red0\\green77\\blue187;\\red127\\green127\\blue127;}\r\n");
      int byteOffset = 0;

      EditorMetaData meta = null;
      byteOffset = AddParts(hex, root, byteOffset, sb, ref meta);

      if(!ReferenceEquals(meta, null) && !ReferenceEquals(meta.Item, null)) {
        while(meta.Item.StoredOffset + meta.Item.StoredLength > byteOffset) {
          sb.Append(hex[byteOffset]).Append(' ');
          byteOffset++;
          meta.Length++;
        }
      }

      meta = (EditorMetaData)item.Tag;
      meta.Length = byteOffset;

      if(hex.Length - 1 > byteOffset) sb.Append("\\cf3 "); // gray
      while(hex.Length-1 > byteOffset) {
        sb.Append(hex[byteOffset]).Append(' ');
        byteOffset++;
      }

      sb.Append("\r\n}\r\n");
      richTextBox1.Rtf = sb.ToString();
    }

    private int AddParts(string[] hex, TreeNode node, int byteOffset, StringBuilder sb, ref EditorMetaData previousMeta) {
      MsgPackItem item = (MsgPackItem)node.Tag;
      if(ReferenceEquals(item, null)) return byteOffset;
      int additionalBytes = 0;
      while(item.StoredOffset > byteOffset) {
        sb.Append(hex[byteOffset]).Append(' ');
        byteOffset++;
        additionalBytes++;
      }
      if(additionalBytes > 0) {
        previousMeta.Length+= additionalBytes;
        TreeNode parent = previousMeta.Node.Parent;
        while(!ReferenceEquals(parent, null)) {
          ((EditorMetaData)((MsgPackItem)parent.Tag).Tag).Length += additionalBytes;
          parent = parent.Parent;
        }
      }

      EditorMetaData meta = new EditorMetaData() {
        CharOffset = byteOffset * 3,
        Node = node,
        Item = item
      };
      lineairList.Add(meta);
      item.Tag = meta;
      previousMeta = meta;

      if(!ReferenceEquals(item, null) && !(item is MpError && !ReferenceEquals(((MpError)item).PartialItem, null)) && byteOffset<hex.Length) {
        sb.Append("\\cf1 "); // red
        sb.Append(hex[byteOffset]).Append(' ');
        byteOffset++;
      } 

      if(item is MsgPackVarLen) {
        int lengthBytes = 0;
        switch(item.TypeId) {
          case MsgPackTypeId.MpBin8: lengthBytes = 1; break;
          case MsgPackTypeId.MpBin16: lengthBytes = 2; break;
          case MsgPackTypeId.MpBin32: lengthBytes = 4; break;
          case MsgPackTypeId.MpStr8: lengthBytes = 1; break;
          case MsgPackTypeId.MpStr16: lengthBytes = 2; break;
          case MsgPackTypeId.MpStr32: lengthBytes = 4; break;
          case MsgPackTypeId.MpMap16: lengthBytes = 2; break;
          case MsgPackTypeId.MpMap32: lengthBytes = 4; break;
          case MsgPackTypeId.MpArray16: lengthBytes = 2; break;
          case MsgPackTypeId.MpArray32: lengthBytes = 4; break;
          case MsgPackTypeId.MpExt8: lengthBytes = 1; break;
          case MsgPackTypeId.MpExt16: lengthBytes = 2; break;
          case MsgPackTypeId.MpExt32: lengthBytes = 4; break;
        }
        if(lengthBytes > 0) {
          sb.Append("\\cf2 "); // blue
          for(int t = lengthBytes - 1; t >= 0; t--) {
            sb.Append(hex[byteOffset]).Append(' ');
            byteOffset++;
          }
        }
      }
      sb.Append("\\cf0 "); // black

      for(int t = 0; t < node.Nodes.Count; t++) {
        byteOffset = AddParts(hex, node.Nodes[t], byteOffset, sb, ref previousMeta);
      }

      ValidateItem(meta);

      meta.Length = (byteOffset - (int)item.StoredOffset);
      return byteOffset;
    }
    
    private TreeNode GetTreeNodeFor(MsgPackItem item) {
      int imgIdx = GetIconFor(item);
      string text = ReferenceEquals(item, null) ? "NULL" : item.ToString();
      int pos = text.IndexOfAny(new char[] { '\r', '\n' });
      if(pos > 0) text = text.Substring(0, pos - 1);
      TreeNode node = new TreeNode(text, imgIdx, imgIdx);
      if(ReferenceEquals(item, null) || item.IsBestGuess) node.ForeColor = Color.DarkGray;
      node.Tag = item;
      return node;
    }

    private void Traverse(TreeNode node, MsgPackItem item) {
      if(ReferenceEquals(item, null)) return;
      Type typ = item.GetType();
      if(typ == typeof(MpBool)) return;
      if(typ == typeof(MpInt)) return;
      if(typ == typeof(MpFloat)) return;
      if(typ == typeof(MpBin)) return;
      if(typ == typeof(MpString)) return;
      if(typ == typeof(MpArray)) {
        MpArray arr = (MpArray)item;
        MsgPackItem[] children = arr.PackedValues;
        for(int t = 0; t < children.Length; t++) {
          TreeNode child = GetTreeNodeFor(children[t]);
          node.Nodes.Add(child);
          Traverse(child, children[t]);
        }
      }
      if(typ == typeof(MpMap)) {
        MpMap map = (MpMap)item;
        KeyValuePair<MsgPackItem, MsgPackItem>[] children = map.PackedValues;
        for(int t = 0; t < children.Length; t++) {
          TreeNode child = GetTreeNodeFor(children[t].Key);
          child.StateImageIndex = 8; // Key
          node.Nodes.Add(child);
          Traverse(child, children[t].Key);
          TreeNode childVal = GetTreeNodeFor(children[t].Value);
          childVal.StateImageIndex = 9; // Value
          child.Nodes.Add(childVal);
          Traverse(childVal, children[t].Value);
        }
      }
      if(typ == typeof(MpError)) {
        MpError err = (MpError)item;
        if(!ReferenceEquals(err.PartialItem, null)) {
          if(!(err.PartialItem is MpError)) node.StateImageIndex = GetIconFor(err.PartialItem);
          TreeNode child = GetTreeNodeFor(err.PartialItem);
          node.Nodes.Add(child);
          Traverse(child, err.PartialItem);
        }
      }
    }

    private int GetIconFor(MsgPackItem item) {
      if(ReferenceEquals(item, null)) return 0;
      Type typ = item.GetType();
      if(typ == typeof(MpBool)) return 1;
      if(typ == typeof(MpInt)) return 2;
      if(typ == typeof(MpFloat)) return 3;
      if(typ == typeof(MpBin)) return 4;
      if(typ == typeof(MpString)) return 5;
      if(typ == typeof(MpArray)) return 6;
      if(typ == typeof(MpMap)) return 7;
      if(typ == typeof(MpExt)) return 10;
      if(typ == typeof(MpError)) return 11;
      return -1;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    private const int WM_SETREDRAW = 0x0b;
    
    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {
      if(ReferenceEquals(e.Node, null)) {
        propertyGrid1.SelectedObject = null;
        ColorSelectedNodeInHexView(null);
        ClearValidationSelection();
      } else {
        propertyGrid1.SelectedObject = e.Node.Tag;
        propertyGrid1.ExpandAllGridItems();

        MsgPackItem item = e.Node.Tag as MsgPackItem;
        if(ReferenceEquals(item, null)) {
          statusOffset.Text = "0 (0x00)";
          ColorSelectedNodeInHexView(null);
        } else {
          statusOffset.Text = string.Concat(item.StoredOffset, " (0x", item.StoredOffset.ToString("X"), ")");
          EditorMetaData meta = item.Tag as EditorMetaData;
          ColorSelectedNodeInHexView(meta);
        }

        if(!lvSelecting) {
          lvSelecting = true;
          try {
            for(int t = listView1.Items.Count - 1; t >= 0; t--) {
              bool select = listView1.Items[t].Tag == e.Node;
              listView1.Items[t].Selected = select;
            }
          } finally {
            lvSelecting = false;
            errorDetails.Visible = false;
            splitter4.Visible = false;
          }
        }
      }
    }

    private void ClearValidationSelection() {
      for(int t = listView1.Items.Count - 1; t >= 0; t--) {
        listView1.Items[t].Selected = false;
      }
    }

    private void ColorSelectedNodeInHexView(EditorMetaData meta) {
      int preserveSelStart = richTextBox1.SelectionStart;
      int preserveSelLength = richTextBox1.SelectionLength;
      bool preserveSelecting = rtbSelecting;

      rtbSelecting = true;
      SendMessage(richTextBox1.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
      try {
        richTextBox1.SelectAll();
        richTextBox1.SelectionBackColor = richTextBox1.BackColor;

        if(!ReferenceEquals(meta, null)) {
          richTextBox1.SelectionStart = meta.CharOffset;
          richTextBox1.SelectionLength = meta.Length * 3;

          richTextBox1.SelectionBackColor = Color.LightGreen;
        }

        if(!preserveSelecting) {
          richTextBox1.ScrollToCaret();
          richTextBox1.SelectionLength = 0;
        } else {
          richTextBox1.SelectionStart = preserveSelStart;
          richTextBox1.SelectionLength = preserveSelLength;
        }
      } finally {
        SendMessage(richTextBox1.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
        richTextBox1.Invalidate();
        rtbSelecting = preserveSelecting;
      }
    }

    bool rtbSelecting = false;
    private void richTextBox1_SelectionChanged(object sender, EventArgs e) {
      if(rtbSelecting) return;
      rtbSelecting = true;
      try {
        for(int t = lineairList.Count - 1; t >= 0; t--) {
          if(lineairList[t].CharOffset <= richTextBox1.SelectionStart) {
            treeView1.SelectedNode = lineairList[t].Node;
            return;
          }
        }
      } finally {
        rtbSelecting = false;
      }
    }

    private void richTextBox1_MouseDownTreeView(Object sender, MouseEventArgs e) {
      if (e.Button == System.Windows.Forms.MouseButtons.Right) {
        treeView1.SelectedNode = treeView1.GetNodeAt(e.X, e.Y);
        if (this.PopulateContextMenu(treeView1.SelectedNode)) {
          contextMenu.Show(treeView1, e.Location);
        }
      }
    }

    private bool lvSelecting = false;
    private void listView1_SelectedIndexChanged(object sender, EventArgs e) {
      if(lvSelecting) return;
      lvSelecting = true;
      try {
        if(listView1.SelectedItems.Count <= 0) {
          treeView1.SelectedNode = null;
          errorDetails.Visible = false;
          splitter4.Visible = false;
          return;
        }
        errorDetails.Text = listView1.SelectedItems[0].SubItems[1].Text;
        splitter4.Visible = true;
        errorDetails.Visible = true;
        treeView1.SelectedNode = (TreeNode)listView1.SelectedItems[0].Tag;      
      } finally {
        lvSelecting = false;
      }
    }

    private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e) {
      e.DrawDefault = true;
    }

    private void ValidateItem(EditorMetaData meta) {
      MsgPackValidation.ValidationItem[] issues = MsgPackValidation.ValidateItem(meta.Item);
      for(int t = issues.Length - 1; t >= 0; t--) {
        AddValidationItem(issues[t].WaistedBytes, meta, issues[t].Severity, issues[t].Message);
      }
    }
    
    private void AddValidationItem(int waistedBytes, EditorMetaData meta, MsgPackValidation.ValidationSeverity sev, string message) {
      int iconId = (meta.Item.TypeId == MsgPackTypeId.NeverUsed) ? -1 : GetIconFor(meta.Item);
      ListViewItem lvi = new ListViewItem(waistedBytes.ToString(), iconId);
      lvi.StateImageIndex = (int)sev;
      
      lvi.SubItems.Add(message);
      lvi.Tag = meta.Node;
      listView1.Items.Add(lvi);
    }

    private bool PopulateContextMenu(Object objClicked) {
      bool rv = false;
      if (objClicked is TreeNode) {
        contextMenu.MenuItems.Clear();
        contextMenu.MenuItems.Add(new MenuItem("Save payload as...", new System.EventHandler(this.onSavePayloadClick)));
        contextMenu.MenuItems.Add(new MenuItem("Parse payload as MessagePack", new System.EventHandler(this.onParseAsMsgPackClick)));
        rv = true;
      }

      return rv;
    }

    private void onParseAsMsgPackClick(object sender, EventArgs e) {
      if (treeView1.SelectedNode != null) {
        MsgPackItem item = treeView1.SelectedNode.Tag as MsgPackItem;

        if (item != null && item.Value != null) {
          PayloadMsgPackExplorer payloadExplorer = new PayloadMsgPackExplorer(item.Value as byte[]);
          payloadExplorer.Text = "Payload Explorer: " + treeView1.SelectedNode.Text;

          payloadExplorer.ShowDialog();
          payloadExplorer.Dispose();
        }
      }
    }

    private void onSavePayloadClick(Object sender, System.EventArgs e) {
      if (treeView1.SelectedNode != null) {
        MsgPackItem item = treeView1.SelectedNode.Tag as MsgPackItem;

        if (item != null) {
          SaveFileDialog saveFileDialog1 = new SaveFileDialog();

          saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
          saveFileDialog1.FilterIndex = 2;
          saveFileDialog1.RestoreDirectory = true;
          bool written = false;

          if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
            Stream writeStream;
            if ((writeStream = saveFileDialog1.OpenFile()) != null) {
              using (BinaryWriter writer = new BinaryWriter(writeStream)) {
                byte[] bytes = item.Value as byte[];
                if (null != bytes) {
                  writer.Write(bytes);
                  written = true;
                }
              }
              writeStream.Close();
            }
            if (!written) {
              MessageBox.Show("No data written", "Problem");
            }
          }
        }
      }
    }
  }
}
