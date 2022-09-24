using System.Text;

namespace KAIFreeAudiencesBot.Services;

public class Table : IDisposable
    {
        private readonly StringBuilder _tableStringBuilder;

        public Table(StringBuilder tableStringBuilder, string id = "default", string classValue="")
        {
            _tableStringBuilder = tableStringBuilder;
            _tableStringBuilder.Append($"<table id=\"{id}\" class=\"{classValue}\">\n");
        }

        public void Dispose()
        {
            _tableStringBuilder.Append("</table>");
        }

        public Row AddRow()
        {
            return new Row(_tableStringBuilder);
        }

        public Row AddHeaderRow()
        {
            return new Row(_tableStringBuilder, true);
        }

        public void StartTableBody()
        {
            _tableStringBuilder.Append("<tbody>");
        }

        public void EndTableBody()
        {
            _tableStringBuilder.Append("</tbody>");
        }
    }

public class Row : IDisposable
{
    private StringBuilder _sb;
    private bool _isHeader;

    public Row(StringBuilder sb, bool isHeader = false)
    {
        _sb = sb;
        _isHeader = isHeader;
        if (_isHeader)
        {
            _sb.Append("<thead>\n");
        }

        _sb.Append("\t<tr>\n");
    }

    public void Dispose()
    {
        _sb.Append("\t</tr>\n");
        if (_isHeader)
        {
            _sb.Append("</thead>\n");
        }
    }

    public void CloseRow()
    {
        _sb.Append("\t</tr>\n");
    }
    public void CloseHeaderRow()
    {
        _sb.Append("\t</tr>\n");
        _sb.Append("</thead>\n");
    }

    public void AddCell(string innerText)
    {
        _sb.Append("\t\t<td>\n");
        _sb.Append("\t\t\t" + innerText);
        _sb.Append("\t\t</td>\n");
    }
}