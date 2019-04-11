using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shipwreck.Postal
{
    internal sealed class CsvReader : IDisposable
    {
        private enum State : byte
        {
            NewField,
            Cr,
            Plain,
            Quoted,
            Escaping,
        }

        private TextReader _Reader;
        private bool _LeaveOpen;

        public CsvReader(TextReader reader, bool leaveOpen = false)
        {
            _Reader = reader;
            _LeaveOpen = leaveOpen;
        }

        private int _NextChar = -1;
        private StringBuilder _Buffer;
        private List<string> _Fields;

        public int FieldCount => _Fields?.Count ?? 0;

        public string this[int index]
            => index < _Fields?.Count ? _Fields[index] : null;

        public bool Read()
        {
            if (_Reader == null)
            {
                throw new ObjectDisposedException("_Reader");
            }
            var s = State.NewField;
            _Buffer?.Clear();
            _Fields?.Clear();

            for (; ; )
            {
                var ci = _NextChar >= 0 ? _NextChar : _Reader.Read();
                _NextChar = -1;

                if (ci < 0)
                {
                    switch (s)
                    {
                        case State.Plain:
                        case State.Quoted:
                        case State.Escaping:
                            PushField();
                            break;
                    }
                    return _Fields?.Count > 0;
                }

                var c = (char)ci;

                switch (s)
                {
                    case State.NewField:
                        switch (c)
                        {
                            case '\r':
                                s = State.Cr;
                                break;

                            case '\n':
                                return true;

                            case '"':
                                s = State.Quoted;
                                break;

                            case ',':
                                (_Fields ?? (_Fields = new List<string>())).Add(string.Empty);
                                break;

                            default:
                                s = State.Plain;
                                (_Buffer ?? (_Buffer = new StringBuilder())).Append(c);
                                break;
                        }
                        break;

                    case State.Cr:
                        _NextChar = c == '\n' ? -1 : ci;
                        return true;

                    case State.Plain:
                        switch (c)
                        {
                            case '\r':
                                PushField();
                                s = State.Cr;
                                break;

                            case '\n':
                                PushField();
                                return true;

                            case ',':
                                PushField();
                                s = State.NewField;
                                break;

                            default:
                                PushChar(c);
                                break;
                        }
                        break;

                    case State.Quoted:
                        switch (c)
                        {
                            case '"':
                                s = State.Escaping;
                                break;

                            default:
                                PushChar(c);
                                break;
                        }
                        break;

                    case State.Escaping:
                        switch (c)
                        {
                            case '\r':
                                PushField();
                                s = State.Cr;
                                break;

                            case '\n':
                                PushField();
                                return true;

                            case ',':
                                PushField();
                                s = State.NewField;
                                break;

                            default: // valid only for '"'
                                PushChar(c);
                                s = State.Quoted;
                                break;
                        }
                        break;
                }
            }
        }

        public bool ReadAndCopyTo(ref string[] fields)
        {
            if (Read())
            {
                if (_Fields == null)
                {
                    if (fields?.Length != 0)
                    {
                        fields = new string[0];
                    }
                }
                else
                {
                    CopyTo(ref fields);
                }
                return true;
            }
            return false;
        }

        public bool CopyTo(ref string[] fields)
        {
            if (_Fields == null)
            {
                return false;
            }

            if (_Fields.Count == (fields?.Length ?? -1))
            {
                _Fields.CopyTo(fields);
            }
            else
            {
                fields = _Fields.ToArray();
            }
            return true;
        }

        private void PushChar(char c) => (_Buffer ?? (_Buffer = new StringBuilder())).Append(c);

        private void PushField()
        {
            (_Fields ?? (_Fields = new List<string>())).Add(_Buffer?.ToString() ?? string.Empty);
            _Buffer?.Clear();
        }

        public void Dispose()
        {
            if (!_LeaveOpen)
            {
                _Reader?.Dispose();
                _Reader = null;
            }
        }
    }
}