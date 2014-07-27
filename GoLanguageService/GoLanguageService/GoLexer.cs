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
		private string m_source;

		private int m_offset;

		private int m_readOffset;

		private char m_character;

		private bool m_insertSemicolon;

		private bool m_eof;

		public GoLexer()
		{
			this.Reset();
		}

		public void SetSource( string source, int offset )
		{
			this.Reset();
			m_source = source;
			m_offset = offset;
		}

		public GoToken GetToken( ref int state )
		{
			var token = new GoToken();

			this.SkipWhitespace();

			token.Position = m_offset;

			var insertSemicolon = false;
			var ch = m_character;
			if( IsLetter( ch ) )
			{
				token.Text = this.ScanIdentifier();
				if( token.Text.Length > 1 )
				{
					token.ID = LookUpToken( token.Text );
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
				}
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
						if( '0' <= m_character && m_character <= '9' )
						{
							insertSemicolon = true;
							this.ScanNumber( out token.ID, out token.Text, true );
						}
						else if( m_character == '.' )
						{
							this.Next();
							if( m_character == '.' )
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
						if( m_character == '/' || m_character == '*' )
						{
							if( m_insertSemicolon && this.FindLineEnd() )
							{
								m_character = '/';
								m_offset = token.Position;
								m_readOffset = m_offset + 1;
								m_insertSemicolon = false;
								token.ID = GoTokenID.SEMICOLON;
								token.Text = "\n";
								return token;
							}
							token.Text = this.ScanComment();
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
						if( m_character == '-' )
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
						if( m_character == '^' )
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

			m_insertSemicolon = insertSemicolon;

			return token;
		}

		private GoTokenID Switch4( GoTokenID goTokenID1, GoTokenID goTokenID2, char p, GoTokenID goTokenID3, GoTokenID goTokenID4 )
		{
			throw new NotImplementedException();
		}

		private string ScanComment()
		{
			throw new NotImplementedException();
		}

		private bool FindLineEnd()
		{
			throw new NotImplementedException();
		}

		private GoTokenID Switch3( GoTokenID goTokenID1, GoTokenID goTokenID2, char p, GoTokenID goTokenID3 )
		{
			throw new NotImplementedException();
		}

		private GoTokenID Switch2( GoTokenID goTokenID1, GoTokenID goTokenID2 )
		{
			throw new NotImplementedException();
		}

		private string ScanRawString()
		{
			throw new NotImplementedException();
		}

		private string ScanCharacter()
		{
			throw new NotImplementedException();
		}

		private string ScanString()
		{
			throw new NotImplementedException();
		}

		private static GoTokenID LookUpToken( string lit )
		{
			throw new NotImplementedException();
		}

		private static bool IsLetter( char ch )
		{
			throw new NotImplementedException();
		}

		private void Reset()
		{
			m_source = "";
			m_offset = 0;
			m_character = ' ';
			m_insertSemicolon = false;
		}

		private void SkipWhitespace()
		{
			while( m_character == ' ' ||
				m_character == '\t' || 
				( m_character == '\n' && m_insertSemicolon == false ) ||
				m_character == '\r' )
			{
				this.Next();
			}
		}

		private void Next()
		{
			throw new NotImplementedException();
		}

		private string ScanIdentifier()
		{
			throw new NotImplementedException();
		}

		private void ScanNumber( out GoTokenID p1, out string lit, bool p2 )
		{
			throw new NotImplementedException();
		}

	}
}
