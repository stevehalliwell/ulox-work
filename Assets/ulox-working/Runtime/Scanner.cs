using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ULox
{
    //TODO see challenges
    public class Scanner
    {
        public List<Token> Tokens { get; private set; }
        private int _line, _characterNumber;
        private StringReader _stringReader;
        private StringBuilder workingSpaceStringBuilder;
        private Char _currentChar;
        private Action<string> _logger;
        private Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "var",    TokenType.VAR},
            { "string", TokenType.STRING},
            { "int",    TokenType.INT},
            { "float",  TokenType.FLOAT},
            { "and",    TokenType.AND},
            { "or",     TokenType.OR},
            { "if",     TokenType.IF},
            { "else",   TokenType.ELSE},
            { "while",  TokenType.WHILE},
            { "for",    TokenType.FOR},
             //todo { "loop",   TokenType.LOOP},
            { "return", TokenType.RETURN},
            //todo { "break",  TokenType.BREAK},
            //todo { "continue", TokenType.CONTINUE},
            { "true",   TokenType.TRUE},
            { "false",  TokenType.FALSE},
            { "null",   TokenType.NULL},
            { "print",  TokenType.PRINT},
            { "fun",  TokenType.FUNCTION},
            { "class",  TokenType.CLASS},
            { ".",  TokenType.DOT},
            { "this",  TokenType.THIS},
            { "super",  TokenType.SUPER},
        };

        public Scanner(Action<string> logger)
        {
            _logger = logger;
            Reset();
        }

        public void Reset()
        {
            Tokens = new List<Token>();
            _line = 0;
            _characterNumber = 0;
            if (_stringReader != null)
                _stringReader.Dispose();
            workingSpaceStringBuilder = new StringBuilder();
        }

        public void Scan(string text)
        {
            using (_stringReader = new StringReader(text))
            {
                while (!IsAtEnd())
                {
                    Advance();

                    //TODO add basic logic symbols
                    switch (_currentChar)
                    {
                    case '(': AddTokenSingle(TokenType.OPEN_PAREN); break;
                    case ')': AddTokenSingle(TokenType.CLOSE_PAREN); break;
                    case '{': AddTokenSingle(TokenType.OPEN_BRACE); break;
                    case '}': AddTokenSingle(TokenType.CLOSE_BRACE); break;
                    case ',': AddTokenSingle(TokenType.COMMA); break;
                    case '.': AddTokenSingle(TokenType.DOT); break;
                    case '-': AddTokenSingle(TokenType.MINUS); break;
                    case '+': AddTokenSingle(TokenType.PLUS); break;
                    case ';': AddTokenSingle(TokenType.END_STATEMENT); break;
                    case '*': AddTokenSingle(TokenType.STAR); break;
                    case '%': AddTokenSingle(TokenType.PERCENT); break;

                    case '!': AddTokenSingle(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                    case '=': AddTokenSingle(Match('=') ? TokenType.EQUALITY : TokenType.ASSIGN); break;
                    case '<': AddTokenSingle(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                    case '>': AddTokenSingle(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                    case '/':
                        {
                            if (Match('/'))
                            {
                                _stringReader.ReadLine();
                                _line++;
                            }
                            else if(Match('*'))
                            {
                                ConsumeBlockComment();
                            }
                            else
                            {
                                AddTokenSingle(TokenType.SLASH);
                            }
                            break;
                        }

                    case ' ':
                    case '\r':
                    case '\t':
                        //skiping over whitespace
                        _characterNumber++;
                        break;

                    case '\n':
                        _line++;
                        _characterNumber = 0;
                        break;

                    case '"':ConsumeString();break;

                    default:
                        {
                            if(IsDigit(_currentChar))
                            {
                                ConsumeNumber();
                            }
                            else if (IsAlpha(_currentChar))
                            {
                                ConsumeIdentifier();
                            }
                            else
                            {
                                Error($"Unexpected character '{(char)_currentChar}'"); 
                            }
                            break;
                        }
                    }
                }

                Tokens.Add(new Token(TokenType.EOF, "", null, _line, _characterNumber));
            }
        }

        //TODO test this
        private void ConsumeBlockComment()
        {
            while(!IsAtEnd())
            {
                if (Match('*') && Match('/'))
                {
                    return;
                }
                else
                {
                    Advance();
                }
            }
        }

        private void ConsumeIdentifier()
        {
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(_currentChar);

            while (IsAlphaNumber(Peek()))
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
            }

            var identString = workingSpaceStringBuilder.ToString();

            if (keywords.TryGetValue(identString, out var keywordTokenType))
                AddToken(keywordTokenType, identString);
            else
                AddToken(TokenType.IDENTIFIER, identString);
        }

        private void ConsumeNumber()
        {
            bool hasFoundDecimalPoint = false;
            workingSpaceStringBuilder.Clear();
            
            workingSpaceStringBuilder.Append(_currentChar);

            while (IsDigit(Peek()))
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
            }


            if (Peek() == '.')
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
                hasFoundDecimalPoint = true;
                while (IsDigit(Peek()))
                {
                    Advance();
                    workingSpaceStringBuilder.Append(_currentChar);
                }
            }

            AddToken(hasFoundDecimalPoint ? TokenType.FLOAT : TokenType.INT, 
                //todo eventually we want to be smarter for now
               double.Parse(workingSpaceStringBuilder.ToString()));
        }

        private void ConsumeString()
        {
            //todo smarter
            var startingLine = _line;
            var startingChar = _characterNumber;
            workingSpaceStringBuilder.Clear();
            Advance();//skip leading " 
            while (_currentChar > 0)
            {
                if (_currentChar == '\n') _line++;

                if(_currentChar == '"')
                {
                    //TODO unescape characters
                    AddToken(TokenType.STRING, workingSpaceStringBuilder.ToString());
                    return;
                }

                workingSpaceStringBuilder.Append(_currentChar);

                Advance();
            }

            Error("Unterminated String", startingLine, startingChar);
        }

        private void Advance()
        {
            _currentChar = (Char)_stringReader.Read();
            _characterNumber++;
        }

        private static bool IsDigit(int ch)
        {
            return ch >= '0' && ch <= '9';
        }

        private bool IsAlpha(int c)
        {
            return (c >= 'a' && c <= 'z') ||
              (c >= 'A' && c <= 'Z') ||
               c == '_';
        }

        private bool IsAlphaNumber(int c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        private bool IsAtEnd()
        {
            return _stringReader.Peek() == -1;
        }

        private Char Peek()
        {
            return (Char)_stringReader.Peek();
        }

        private bool Match(Char matchingCharToConsume)
        {
            if (_stringReader.Peek() == matchingCharToConsume)
            {
                if(_stringReader.Read() == '\n')
                {
                    _line++;
                    _characterNumber = 0;
                }
                _characterNumber++;

                return true;
            }
            return false;
        }

        private void Error(string v)
        {
            Error(v, _line, _characterNumber);
        }

        private void Error(string v, int lineNumber, int characterNumber)
        {
            _logger.Invoke($"{v}|{lineNumber}:{characterNumber}");
        }


        //was own function but lexeme as litteral made more sense at the time
        private void AddTokenSingle(TokenType simpleToken)
        {
            Tokens.Add(new Token(simpleToken, _currentChar.ToString(), string.Empty, _line, _characterNumber));
        }

        private void AddToken(TokenType simpleToken, object literal)
        {
            Tokens.Add(new Token(simpleToken, literal.ToString(), literal, _line, _characterNumber));
        }
    }
}