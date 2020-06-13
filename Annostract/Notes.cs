using System;
using System.Collections.Generic;

namespace Annostract {
    public class Note
    {
    }

    public class TreeNote : TextNote
    {
        public TreeNote(string content) : base(content)
        {
            Children = new List<TreeNote>();
        }
        public TreeNote? Parent { get; set; }

        public List<TreeNote> Children { get; set; }
    }

    public class TextNote : Note {
        public string Content { get; internal set; }

        public TextNote(string content)
        {
            Content = content;
        }
    }

    public class Quote : TextNote { public Quote(string content) : base(content) {} }
    public class ToRead : TextNote { public ToRead(string content) : base(content) {} }
    public class Abstract : TextNote { public Abstract(string content) : base(content) {} }
    public class DoiLink : TextNote { public DoiLink(string content) : base(content) { } }
    public class YearNote : TextNote { public YearNote(string content) : base(content) { } }

    public class HighlightNote : Note
    {
        public string highlightedText;
        public string? note;

        public HighlightNote(string highlightedText, string? note)
        {
            this.highlightedText = highlightedText;
            this.note = note;
        }

        internal TreeNote ToTextNote(TreeNote parent)
        {
            var res = new TreeNote(highlightedText) {
                Parent = parent
            };
            parent.Children.Add(res);
            return res;
        }
    }
}