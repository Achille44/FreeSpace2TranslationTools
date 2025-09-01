using FreeSpace2TranslationTools.Exceptions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Files
{
    internal class VisualNovel : IFile
    {
        private const string VISUAL_NOVEL_INITIAL_MARKER = "FILEVERSION";
        private const string EMPTY_TEXT = "text=\" \"";
        private const string TEXT = "text=";
        private const string DEFAULT_XSTR_ID_MARKER = "xstrid";
        private const string DEFAULT_XSTR_ID = "-1";
        private const string MSGXSTR_MARKER = "MSGXSTR";
        private const string SHOWICON_MARKER = "SHOWICON";
        private const string SEPARATOR = " ";
        private string Content;
        private readonly string[] ContentLines;

        internal VisualNovel(string content)
        {
            if (!content.Trim().StartsWith(VISUAL_NOVEL_INITIAL_MARKER))
            {
                throw new WrongFileFormatException();
            }

            Content = content;

            ContentLines = content.Split(Environment.NewLine);
        }

        public string GetInternationalizedContent()
        {
            foreach (string line in ContentLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                switch (line.Split(' ')[0])
                {
                    case "GOTO":
                    case "SETINT":
                    case "FORCEINT":
                    case "INTRETURN":
                    case "FILEVERSION":
                    case "PRELOADGRAPHICS":
                    case "LABEL":
                    case "ACTION":
                    case "PLAY":
                    case "STOPMUSIC":
                    case "SHOW":
                    case "SHOWDISPLAY":
                    case "HIDEDISPLAY":
                    case "MOVE":
                    case "CHANGE":
                    case "HIDE":
                    case "hide":
                    case "SETBG":
                    case "LOADMAP":
                    case "SETMAPICON":
                    case "HIDEMAPICON":
                    case "SHOWMAP":
                    case "MENU":
                    case "IF":
                    case "EIF":
                    case "ELSE":
                    case "ENDIF":
                    case "END":
                    case "WAIT":
                    case "SETVAR":
                    case "SETSEXPVAR":
                    case "SETFLAG":
                    case "SETFONT":
                    case "MSGXSTR":
                        // nothing to internationalize here (for now)
                        break;
                    case SHOWICON_MARKER:
                        InternationalizeShowIconLine(line);
                        break;
                    default:
                        InternationalizeMessageLine(line);
                        break;
                }
            }

            return Content;
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            return GetInternationalizedContent();
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            return GetInternationalizedContent();
        }

        private void InternationalizeShowIconLine(string line)
        {
            if (!line.Contains(TEXT) || line.Contains(EMPTY_TEXT) || line.Contains(DEFAULT_XSTR_ID_MARKER))
            {
                return;
            }

            Content = Content.Replace(line, line + $" {DEFAULT_XSTR_ID_MARKER}={DEFAULT_XSTR_ID}");
        }

        private void InternationalizeMessageLine(string line)
        {
            string newLine = MSGXSTR_MARKER;

            MatchCollection strings = Regexp.NotEmptyStrings.Matches(line);

            if (line.StartsWith('\"'))
            {
                newLine += SEPARATOR + strings[0].Value;
                newLine += SEPARATOR + DEFAULT_XSTR_ID;
                newLine += SEPARATOR + strings[1].Value;
                newLine += SEPARATOR + DEFAULT_XSTR_ID;

                if (strings.Count > 2)
                {
                    newLine += SEPARATOR + strings[2].Value;
                }
            }
            else
            {
                newLine += SEPARATOR + $"\"{Regexp.LinesStartingWithAWord.Match(line).Value}\"";
                newLine += SEPARATOR + DEFAULT_XSTR_ID;
                newLine += SEPARATOR + strings[0].Value;
                newLine += SEPARATOR + DEFAULT_XSTR_ID;

                if (strings.Count > 1)
                {
                    newLine += SEPARATOR + strings[1].Value;
                }
            }

            Content = Content.Replace(line, newLine);
        }
    }
}
