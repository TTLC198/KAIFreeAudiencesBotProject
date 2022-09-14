using System.Text;

namespace KAIFreeAudiencesBot.Services;

public class Table : IDisposable
{
    private readonly StringBuilder _tableStringBuilder;

    public const string DefaultStyleTable = @"
        table {
            font-family: 'Lucida Sans Unicode', 'Lucida Grande', Sans-Serif;
            border-collapse: collapse;
            color: #686461;
            margin: 0 auto;
        }
        caption {
            padding: 10px;
            color: white;
            background: #8FD4C1;
            font-size: 18px;
            text-align: left;
            font-weight: bold;
        }
        th {
            border-bottom: 3px solid #B9B29F;
            padding: 10px;
            text-align: center;
        }
        td {
            padding: 10px;
        }
        tr:nth-child(odd) {
            background: white;
        }
        tr:nth-child(even) {
            background: #E8E6D1;
        }
    ";

    public Table(StringBuilder tableStringBuilder, string id = "default", string classValue = "", string style = "")
    {
        _tableStringBuilder = tableStringBuilder;
        _tableStringBuilder.Append($"<style>{style}</style>");
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

    public void AddCell(string innerText)
    {
        _sb.Append("\t\t<td>\n");
        _sb.Append("\t\t\t" + innerText);
        _sb.Append("\t\t</td>\n");
    }
}