namespace Annostract {
    internal class HighlightResult
    {
        private string text;
        private string content;
        private HighlightColor highlightColor;

        public HighlightResult(string text, string content, HighlightColor highlightColor)
        {
            this.text = text;
            this.content = content;
            this.highlightColor = highlightColor;
        }
    }

    public static class ColorConverter
    {

        public static HighlightColor Convert(double r, double g, double b)
        {
            return (r, g, b) switch
            {
                //Microsoft Edge Colours
                (1, 0.90196, 0) => HighlightColor.Yellow,
                (0.26667, 0.78431, 0.96078) => HighlightColor.Blue,
                (0.14902, 0.90196, 0) => HighlightColor.Green,
                (0.92549, 0, 0.54902) => HighlightColor.Pink,
                (_, _, _) => HighlightColor.Unknown
            };
        }

    }

    public enum HighlightColor
    {
        Yellow, Blue, Green, Pink, Unknown
    }
}