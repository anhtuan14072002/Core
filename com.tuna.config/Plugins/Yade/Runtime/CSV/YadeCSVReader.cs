//  Copyright (c) 2022-present amlovey
//  
using System.Collections.Generic;
using System.Text;

namespace Yade.Runtime.CSV
{
    internal class YadeCSVCell
    {
        public string Value;
        public int Row;
        public int Column;

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Row, Column, Value);
        }
    }

    /// <summary>
    /// Yade CSV Reader 
    /// </summary>
    internal class YadeCSVReader
    {
        private string csv;
        private int pos;
        private int contentLength;
        private int row;
        private int column;
        private char delimiter;

        public YadeCSVReader(string csv, bool removeBOM = true, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(csv))
            {
                this.csv = string.Empty;
            }
            else
            {
                if (removeBOM)
                {
                    this.csv = RemoveBOMIfNeeds(csv);
                }
                else
                {
                    this.csv = csv.Replace("\0", "");
                }
            }

            pos = 0;
            row = 0;
            column = 0;
            contentLength = this.csv.Length;
            this.delimiter = delimiter;
        }

        private string RemoveBOMIfNeeds(string s)
        {
            string BOMMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (s.StartsWith(BOMMarkUtf8))
            {
                s = s.Remove(0, BOMMarkUtf8.Length);
            }

            return s.Replace("\0", "");
        }

        internal YadeCSVCell Read()
        {
            if (pos >= contentLength)
            {
                return null;
            }

            bool quoted = false;
            List<char> cellChars = new List<char>();
            for (; pos < contentLength; pos++)
            {
                char ch = csv[pos];

                // For '\r' or '\r\n'
                if (ch == '\r')
                {
                    bool nextCharIsBreak = pos + 1 < contentLength && csv[pos + 1] == '\n';
                    if (nextCharIsBreak)
                    {
                        if (quoted)
                        {
                            pos++;
                            cellChars.Add(ch);
                            cellChars.Add('\n');
                            continue;
                        }
                        else
                        {
                            pos = pos + 2;
                        }

                    }
                    else
                    {
                        pos = pos + 1;

                        if (quoted)
                        {
                            cellChars.Add(ch);
                            continue;
                        }
                    }

                    var cell = GetCell(cellChars, row, column);
                    row++;
                    column = 0;
                    return cell;
                }

                // If only '\n'
                else if (ch == '\n')
                {
                    if (quoted)
                    {
                        cellChars.Add(ch);
                        continue;
                    }

                    pos++;
                    var cell = GetCell(cellChars, row, column);
                    row++;
                    column = 0;
                    return cell;
                }
                else if (ch == '\"')
                {
                    if (quoted)
                    {
                        if (pos + 1 < contentLength)
                        {
                            if (csv[pos + 1] == '\"')
                            {
                                cellChars.Add(ch);
                                pos++;
                            }
                            else
                            {
                                quoted = false;
                            }

                            continue;
                        }

                        continue;
                    }
                    else
                    {
                        quoted = true;
                        continue;
                    }
                }
                else if (ch == this.delimiter)
                {
                    if (quoted)
                    {
                        cellChars.Add(ch);
                        continue;
                    }

                    var cell = GetCell(cellChars, row, column);
                    pos++;
                    column++;
                    return cell;
                }

                cellChars.Add(ch);
            }

            return GetCell(cellChars, row, column);
        }

        private YadeCSVCell GetCell(List<char> chars, int row, int column)
        {
            var cell = new YadeCSVCell();
            cell.Row = row;
            cell.Column = column;
            cell.Value = new string(chars.ToArray());

            return cell;
        }
    }
}
