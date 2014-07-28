using Microsoft.VisualStudio.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	/// <summary>
	/// This is a modified version of go/scanner/scanner.go
	/// </summary>
	public class GoLexer
	{
		public enum State
		{
			Normal,
			InsideComment,
			InsideString,
		}

		public const uint MaxRune = 0x0010FFFF;

		private string m_src;

		private int m_offset;

		private int m_readOffset;

		private char m_ch;

		private bool m_insertSemicolon;

		private bool m_eof;

		public GoLexer()
		{
			this.Reset();
		}

		public void SetSource( string source, int offset )
		{
			this.Reset();
			m_src = source;
			m_offset = offset;

			this.Next();
		}

		public GoToken GetToken( ref int state )
		{
			var token = new GoToken();

			this.SkipWhitespace();

			token.Position = m_offset;

			if( (State)state == State.InsideComment )
			{
				if( m_eof )
				{
					token.ID = GoTokenID.EOF;
					return token;
				}
				var comment = this.ScanComment( ref state );
				token.ID = GoTokenID.COMMENT;
				token.Text = comment;
				return token;
			}

			var insertSemicolon = false;
			var ch = m_ch;
			if( IsLetter( ch ) )
			{
				token.Text = this.ScanIdentifier();
				if( token.Text.Length > 1 )
				{
					token.ID = GoToken.Lookup( token.Text );
					switch( token.ID )
					{
						case GoTokenID.IDENT:
						case GoTokenID.BREAK:
						case GoTokenID.CONTINUE:
						case GoTokenID.FALLTHROUGH:
						case GoTokenID.RETURN:
							insertSemicolon = true;
							break;
					}
				}
				else
				{
					insertSemicolon = true;
					token.ID = GoTokenID.IDENT;
				}
			}
			else if( '0' <= ch && ch <= '9' )
			{
				insertSemicolon = true;
				this.ScanNumber( out token.ID, out token.Text, false );
			}
			else
			{
				this.Next();
				if( m_eof )
				{
					if( m_insertSemicolon )
					{
						m_insertSemicolon = false;
						token.ID = GoTokenID.SEMICOLON;
						token.Text = "\n";
						return token;
					}
					token.ID = GoTokenID.EOF;
				}
				else
				{
					switch( ch )
					{
						case '\n':
							m_insertSemicolon = false;
							token.ID = GoTokenID.SEMICOLON;
							token.Text = "\n";
							return token;
						case '\"':
							insertSemicolon = true;
							token.ID = GoTokenID.STRING;
							token.Text = this.ScanString();
							break;
						case '\'':
							insertSemicolon = true;
							token.ID = GoTokenID.CHAR;
							token.Text = this.ScanCharacter();
							break;
						case '`':
							insertSemicolon = true;
							token.ID = GoTokenID.STRING;
							token.Text = this.ScanRawString();
							break;
						case ':':
							token.ID = this.Switch2( GoTokenID.COLON, GoTokenID.DEFINE );
							break;
						case '.':
							if( '0' <= m_ch && m_ch <= '9' )
							{
								insertSemicolon = true;
								this.ScanNumber( out token.ID, out token.Text, true );
							}
							else if( m_ch == '.' )
							{
								this.Next();
								if( m_ch == '.' )
								{
									this.Next();
									token.ID = GoTokenID.ELLIPSIS;
								}
							}
							else
							{
								token.ID = GoTokenID.PERIOD;
							}
							break;
						case ',':
							token.ID = GoTokenID.COMMA;
							break;
						case ';':
							token.ID = GoTokenID.SEMICOLON;
							token.Text = ";";
							break;
						case '(':
							token.ID = GoTokenID.LPAREN;
							break;
						case ')':
							insertSemicolon = true;
							token.ID = GoTokenID.RPAREN;
							break;
						case '[':
							token.ID = GoTokenID.LBRACK;
							break;
						case ']':
							insertSemicolon = true;
							token.ID = GoTokenID.RBRACK;
							break;
						case '{':
							token.ID = GoTokenID.LBRACE;
							break;
						case '}':
							insertSemicolon = true;
							token.ID = GoTokenID.RBRACE;
							break;
						case '+':
							token.ID = this.Switch3( GoTokenID.ADD, GoTokenID.ADD_ASSIGN, '+', GoTokenID.INC );
							if( token.ID == GoTokenID.INC )
							{
								insertSemicolon = true;
							}
							break;
						case '-':
							token.ID = this.Switch3( GoTokenID.SUB, GoTokenID.SUB_ASSIGN, '-', GoTokenID.DEC );
							if( token.ID == GoTokenID.DEC )
							{
								insertSemicolon = true;
							}
							break;
						case '*':
							token.ID = this.Switch2( GoTokenID.MUL, GoTokenID.MUL_ASSIGN );
							break;
						case '/':
							if( m_ch == '/' || m_ch == '*' )
							{
								if( m_insertSemicolon && this.FindLineEnd() )
								{
									m_ch = '/';
									m_offset = token.Position;
									m_readOffset = m_offset + 1;
									m_insertSemicolon = false;
									token.ID = GoTokenID.SEMICOLON;
									token.Text = "\n";
									return token;
								}
								token.Text = this.ScanComment( ref state );
								token.ID = GoTokenID.COMMENT;
							}
							else
							{
								token.ID = this.Switch2( GoTokenID.QUO, GoTokenID.QUO_ASSIGN );
							}
							break;
						case '%':
							token.ID = this.Switch2( GoTokenID.REM, GoTokenID.REM_ASSIGN );
							break;
						case '^':
							token.ID = this.Switch2( GoTokenID.XOR, GoTokenID.XOR_ASSIGN );
							break;
						case '<':
							if( m_ch == '-' )
							{
								this.Next();
								token.ID = GoTokenID.ARROW;
							}
							else
							{
								token.ID = this.Switch4( GoTokenID.LSS, GoTokenID.LEQ, '<', GoTokenID.SHL, GoTokenID.SHL_ASSIGN );
							}
							break;
						case '>':
							token.ID = this.Switch4( GoTokenID.GTR, GoTokenID.GEQ, '>', GoTokenID.SHR, GoTokenID.SHR_ASSIGN );
							break;
						case '=':
							token.ID = this.Switch2( GoTokenID.ASSIGN, GoTokenID.EQL );
							break;
						case '!':
							token.ID = this.Switch2( GoTokenID.NOT, GoTokenID.NEQ );
							break;
						case '&':
							if( m_ch == '^' )
							{
								this.Next();
								token.ID = this.Switch2( GoTokenID.AND_NOT, GoTokenID.AND_NOT_ASSIGN );
							}
							else
							{
								token.ID = this.Switch3( GoTokenID.AND, GoTokenID.AND_ASSIGN, '&', GoTokenID.LAND );
							}
							break;
						case '|':
							token.ID = this.Switch3( GoTokenID.OR, GoTokenID.OR_ASSIGN, '|', GoTokenID.LOR );
							break;
						default:
							insertSemicolon = m_insertSemicolon;
							token.ID = GoTokenID.ILLEGAL;
							token.Text = ch.ToString();
							break;
					}
				}
			}

			m_insertSemicolon = insertSemicolon;

			return token;
		}

		private void Error( int offset, string message )
		{

		}

		private GoTokenID Switch4( GoTokenID tok0, GoTokenID tok1, char ch2, GoTokenID tok2, GoTokenID tok3 )
		{
			if( m_ch == '=' )
			{
				this.Next();
				return tok1;
			}
			if( m_ch == ch2 )
			{
				this.Next();
				if( m_ch == '=' )
				{
					this.Next();
					return tok3;
				}
				return tok2;
			}
			return tok0;
		}

		private string ScanComment( ref int state )
		{
			var offset = m_offset - 1;
			if( offset == -1 )
			{
				offset = 0;
			}
			var hasCR = false;

			if( m_ch == '/' && state != (int)State.InsideComment )
			{
				this.Next();
				while( m_ch != '\n' && m_eof == false )
				{
					if( m_ch == '\r' )
					{
						hasCR = true;
					}
					this.Next();
				}

				// Do not interpret line comment here.
				// Line comment means comments start with "//line"
				/*
				if( offset == 0 )
				{
					this.InterpretLineComment( m_source.Substring( offset, m_offset - offset ) );
				}
				*/
				goto exit;
			}

			if( state == (int)State.InsideComment )
			{
				if( m_readOffset < m_src.Length )
				{
					var next = m_src[m_readOffset];
					if( m_ch == '*' && next == '/' )
					{
						this.Next();
						this.Next();
						goto exit;
					}
				}
			}

			this.Next();
			while( m_eof == false )
			{
				var ch = m_ch;
				if( ch == '\r' )
				{
					hasCR = true;
				}
				this.Next();
				if( ch == '*' && m_ch == '/' )
				{
					this.Next();
					goto exit;
				}
			}

			m_eof = true;
			state = (int)State.InsideComment;
			var comment = m_src.Substring( offset, m_offset - offset );
			return comment;

			//this.Error( offset, "Comment not terminated" );

		exit:
			var lit = m_src.Substring( offset, m_offset - offset );
			if( hasCR )
			{
				lit = StripCR( lit );
			}

			state = (int)State.Normal;
			return lit;
		}

		private static string StripCR( string lit )
		{
			var result = "";
			foreach( var ch in lit )
			{
				if( ch != '\r' )
				{
					result += ch;
				}
			}
			return result;
		}

		private bool FindLineEnd()
		{
			while( m_ch == '/' || m_ch == '*' )
			{
				if( m_ch == '/' )
				{
					this.FindLineEndDefer( m_offset - 1 );
					return true;
				}
				this.Next();
				while( m_eof == false )
				{
					var ch = m_ch;
					if( ch == '\n' )
					{
						this.FindLineEndDefer( m_offset - 1 );
						return true;
					}
					this.Next();
					if( ch == '*' && m_ch == '/' )
					{
						this.Next();
						break;
					}
				}
				this.SkipWhitespace();
				if( m_eof || m_ch == '\n' )
				{
					this.FindLineEndDefer( m_offset - 1 );
					return true;
				}
				if( m_ch != '/' )
				{
					this.FindLineEndDefer( m_offset - 1 );
					return false;
				}
				this.Next();
			}

			this.FindLineEndDefer( m_offset - 1 );
			return false;
		}

		private void FindLineEndDefer( int offset )
		{
			m_ch = '/';
			m_offset = offset;
			m_readOffset = offset + 1;
			this.Next();
		}

		private GoTokenID Switch3( GoTokenID tok0, GoTokenID tok1, char ch2, GoTokenID tok2 )
		{
			if( m_ch == '=' )
			{
				this.Next();
				return tok1;
			}
			if( m_ch == ch2 )
			{
				this.Next();
				return tok2;
			}
			return tok0;
		}

		private GoTokenID Switch2( GoTokenID tok0, GoTokenID tok1 )
		{
			if( m_ch == '=' )
			{
				this.Next();
				return tok1;
			}
			return tok0;
		}

		private string ScanRawString()
		{
			var offset = m_offset - 1;

			var hasCR = false;
			for( ; ; )
			{
				var ch = m_ch;
				if( m_eof )
				{
					this.Error( offset, "Raw string literal not terminated" );
					break;
				}
				this.Next();
				if( ch == '`' )
				{
					break;
				}
				if( ch == '\r' )
				{
					hasCR = true;
				}
			}

			var lit = m_src.Substring( offset, m_offset - offset );
			if( hasCR )
			{
				lit = StripCR( lit );
			}

			return lit;
		}

		private string ScanCharacter()
		{
			var offset = m_offset - 1;

			var valid = true;
			var n = 0;
			for( ; ; )
			{
				var ch = m_ch;
				if( ch == '\n' || m_eof )
				{
					if( valid )
					{
						this.Error( offset, "Rune literal not terminated" );
						valid = false;
					}
					break;
				}
				this.Next();
				if( ch == '\'' )
				{
					break;
				}
				n++;
				if( ch == '\\' )
				{
					if( this.ScanEscape( '\'' ) == false )
					{
						valid = false;
					}
				}
			}

			if( valid && n != 1 )
			{
				this.Error( offset, "Illegal rune literal" );
			}

			return m_src.Substring( offset, m_offset - offset );
		}

		private bool ScanEscape( char quote )
		{
			var offset = m_offset;

			if( m_ch == quote )
			{
				this.Next();
				return true;
			}

			int n;
			uint b, max;
			switch( m_ch )
			{
				case 'a':
				case 'b':
				case 'f':
				case 'n':
				case 'r':
				case 't':
				case 'v':
				case '\\':
					this.Next();
					return true;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
					n = 3;
					b = 8;
					max = 255;
					break;
				case 'x':
					this.Next();
					n = 2;
					b = 16;
					max = 255;
					break;
				case 'u':
					this.Next();
					n = 4;
					b = 16;
					max = MaxRune;
					break;
				case 'U':
					this.Next();
					n = 8;
					b = 16;
					max = MaxRune;
					break;
				default:
					if( m_eof )
					{
						this.Error( offset, "Escape sequence not terminated" );
					}
					else
					{
						this.Error( offset, "Unknow escape sequence" );
					}
					return false;
			}

			uint x = 0;
			while( n > 0 )
			{
				var d = (uint)DigitVal( m_ch );
				if( d >= b )
				{
					var message = string.Format( "Illegal character {0} in escape sequence", m_ch );
					if( m_eof )
					{
						message = "Escape sequence not terminated";
					}
					this.Error( m_offset, message );
					return false;
				}
				x = x * b + d;
				this.Next();
				n--;
			}

			if( x > max || 0xD800 <= x && x < 0xE000 )
			{
				this.Error( offset, "Escape sequence is invalid Unicode code point" );
				return false;
			}

			return true;
		}

		private int DigitVal( char ch )
		{
			if( m_eof )
			{
				return 16;
			}

			if( '0' <= ch && ch <= '9' )
			{
				return (int)( ch - '0' );
			}
			else if( 'a' <= ch && ch <= 'f' )
			{
				return (int)( ch - 'a' + 10 );
			}
			else if( 'A' <= ch && ch <= 'F' )
			{
				return (int)( ch - 'A' + 10 );
			}
			return 16;
		}

		private string ScanString()
		{
			var offset = m_offset - 1;

			for( ; ; )
			{
				var ch = m_ch;
				if( ch == '\n' || m_eof )
				{
					this.Error( offset, "String literal not terminated" );
					break;
				}
				this.Next();
				if( ch == '\"' )
				{
					break;
				}
				if( ch == '\\' )
				{
					this.ScanEscape( '\"' );
				}
			}

			return m_src.Substring( offset, m_offset - offset );
		}

		private bool IsLetter( char ch )
		{
			if( m_eof )
			{
				return false;
			}
			return 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' || ch == '_' || ( ch >= 0x80 && char.IsLetter( ch ) );
		}

		private void Reset()
		{
			m_src = "";
			m_offset = 0;
			m_readOffset = 0;
			m_ch = ' ';
			m_insertSemicolon = false;
			m_eof = false;
		}

		private void SkipWhitespace()
		{
			while( m_ch == ' ' ||
				m_ch == '\t' ||
				( m_ch == '\n' && m_insertSemicolon == false ) ||
				m_ch == '\r' )
			{
				if( m_eof )
				{
					break;
				}
				this.Next();
			}
		}

		private void Next()
		{
			if( m_readOffset < m_src.Length )
			{
				m_offset = m_readOffset;
				if( m_ch == '\n' )
				{
					/*
					s.lineOffset = s.offset
					s.file.AddLine(s.offset)
					*/
				}
				var r = m_src[m_readOffset];
				var w = 1;
				if( r == 0 )
				{
					this.Error( m_offset, "Illegal character NUL" );
				}
				else if( r >= 0x80 )
				{
					/*
					r, w = utf8.DecodeRune(s.src[s.rdOffset:])
					if r == utf8.RuneError && w == 1 {
						s.error(s.offset, "illegal UTF-8 encoding")
					} else if r == bom && s.offset > 0 {
						s.error(s.offset, "illegal byte order mark")
					}
					*/
				}
				m_readOffset += w;
				m_ch = r;
			}
			else
			{
				m_offset = m_src.Length;
				if( m_ch == '\n' )
				{
					/*
					s.lineOffset = s.offset
					s.file.AddLine(s.offset)
					*/
				}
				m_eof = true;
			}
		}

		private string ScanIdentifier()
		{
			var offset = m_offset;
			while( IsLetter( m_ch ) || IsDigit( m_ch ) )
			{
				this.Next();
			}
			return m_src.Substring( offset, m_offset - offset );
		}

		private bool IsDigit( char ch )
		{
			if( m_eof )
			{
				return false;
			}
			return '0' <= ch && ch <= '9' || ( ch >= 0x80 && char.IsDigit( ch ) );
		}

		private void ScanNumber( out GoTokenID tok, out string lit, bool seenDecimalPoint )
		{
			var offset = m_offset;
			tok = GoTokenID.INT;

			if( seenDecimalPoint )
			{
				offset--;
				tok = GoTokenID.FLOAT;
				this.ScanMantissa( 10 );
				goto exponent;
			}

			if( m_ch == '0' )
			{
				var offs = m_offset;
				this.Next();
				if( m_ch == 'x' || m_ch == 'X' )
				{
					this.Next();
					this.ScanMantissa( 16 );
					if( m_offset - offs <= 2 )
					{
						this.Error( offs, "Illegal hexadecimal number" );
					}
				}
				else
				{
					var seenDecimalDigit = false;
					this.ScanMantissa( 8 );
					if( m_ch == '8' || m_ch == '9' )
					{
						seenDecimalDigit = true;
						this.ScanMantissa( 10 );
					}
					if( m_ch == '.' || m_ch == 'e' || m_ch == 'E' || m_ch == 'i' )
					{
						goto fraction;
					}
					if( seenDecimalDigit )
					{
						this.Error( offs, "Illegal octal number" );
					}
				}
				goto exit;
			}

			this.ScanMantissa( 10 );

		fraction:
			if( m_ch == '.' )
			{
				tok = GoTokenID.FLOAT;
				this.Next();
				this.ScanMantissa( 10 );
			}

		exponent:
			if( m_ch == 'e' || m_ch == 'E' )
			{
				tok = GoTokenID.FLOAT;
				this.Next();
				if( m_ch == '-' || m_ch == '+' )
				{
					this.Next();
				}
				this.ScanMantissa( 10 );
			}

			if( m_ch == 'i' )
			{
				tok = GoTokenID.IMAG;
				this.Next();
			}

		exit:
			lit = m_src.Substring( offset, m_offset - offset );
		}

		private void ScanMantissa( int b )
		{
			while( DigitVal( m_ch ) < b )
			{
				this.Next();
			}
		}

	}
}
