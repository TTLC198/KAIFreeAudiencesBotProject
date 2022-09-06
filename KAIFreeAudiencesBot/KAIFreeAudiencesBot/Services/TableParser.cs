using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace KAIFreeAudiencesBot.Services;

public static class TableParser
{
  public static string ToStringTable<T>(
    this IEnumerable<T> values,
    string?[] columnHeaders,
    params Func<T, object>[] valueSelectors)
  {
    return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
  }

  public static string ToStringTable<T>(
    this IEnumerable<T> values,
    params Expression<Func<T, object>>[] valueSelectors)
  {
    var headers = valueSelectors.Select(func => GetProperty(func).Name).ToArray();
    var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
    return TableParser.ToStringTable(values, headers, selectors);
  }

  private static PropertyInfo GetProperty<T>(Expression<Func<T, object>> expresstion)
  {
    if (expresstion.Body is UnaryExpression)
    {
      if ((expresstion.Body as UnaryExpression)!.Operand is MemberExpression)
      {
        return ((expresstion.Body as UnaryExpression)!.Operand as MemberExpression)!.Member as PropertyInfo;
      }
    }

    if ((expresstion.Body is MemberExpression))
    {
      return (expresstion.Body as MemberExpression)!.Member as PropertyInfo;
    }
    return null!;
  }

  public static string ToStringTable(this string?[,] arrValues)
  {
    int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);
    var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

    var sb = new StringBuilder();
    for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
    {
      for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
      {
        // Print cell
        string? cell = arrValues[rowIndex, colIndex];
        cell = cell.PadRight(maxColumnsWidth[colIndex]);
        sb.Append(" | ");
        sb.Append(cell);
      }

      // Print end of line
      sb.Append(" | ");
      sb.AppendLine();

      // Print splitter
      if (rowIndex == 0)
      {
        sb.AppendFormat(" |{0}| ", headerSpliter);
        sb.AppendLine();
      }
    }

    return sb.ToString();
  }

  private static int[] GetMaxColumnsWidth(string?[,] arrValues)
  {
    var maxColumnsWidth = new int[arrValues.GetLength(1)];
    for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
    {
      for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
      {
        int newLength = arrValues[rowIndex, colIndex]!.Length;
        int oldLength = maxColumnsWidth[colIndex];

        if (newLength > oldLength)
        {
          maxColumnsWidth[colIndex] = newLength;
        }
      }
    }

    return maxColumnsWidth;
  }
}
