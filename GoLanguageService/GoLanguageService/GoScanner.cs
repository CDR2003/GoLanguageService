using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoScanner : IScanner
	{
		private GoLexer m_lexer;

		public GoScanner()
		{
			m_lexer = new GoLexer();
		}

		public bool ScanTokenAndProvideInfoAboutIt( TokenInfo tokenInfo, ref int state )
		{
			GoToken token = null;
			do
			{
				token = m_lexer.GetToken( ref state );
			} while( token.Text == "\n" && token.ID == GoTokenID.SEMICOLON );

			if( token.ID == GoTokenID.EOF )
			{
				return false;
			}

			if( string.IsNullOrEmpty( token.Text ) )
			{
				token.Text = GoToken.ToString( token.ID );
			}

			tokenInfo.StartIndex = token.Position;
			tokenInfo.EndIndex = tokenInfo.StartIndex + token.Text.Length - 1;
			tokenInfo.Token = (int)token.ID;
			tokenInfo.Type = GetType( token.ID );
			tokenInfo.Color = GetColor( tokenInfo.Type );
			tokenInfo.Trigger = GetTriggers( token.ID );
			return true;
		}

		public void SetSource( string source, int offset )
		{
			var file = new GoSourceFile( new GoSourceFileSet(), "", offset, source.Length );
			m_lexer.SetSource( file, source, offset );
		}

		private static TokenType GetType( GoTokenID id )
		{
			if( GoToken.IsKeyword( id ) )
			{
				return TokenType.Keyword;
			}
			if( GoToken.IsOperator( id ) )
			{
				return TokenType.Operator;
			}
			if( GoToken.IsLiteral( id ) )
			{
				if( id == GoTokenID.STRING || id == GoTokenID.CHAR )
				{
					return TokenType.String;
				}
				return TokenType.Literal;
			}
			if( id == GoTokenID.COMMENT )
			{
				return TokenType.Comment;
			}
			if( id == GoTokenID.IDENT )
			{
				return TokenType.Identifier;
			}
			return TokenType.Unknown;
		}

		private static TokenColor GetColor( TokenType type )
		{
			switch( type )
			{
				case TokenType.Comment:
				case TokenType.LineComment:
					return TokenColor.Comment;
				case TokenType.Identifier:
					return TokenColor.Identifier;
				case TokenType.Keyword:
					return TokenColor.Keyword;
				case TokenType.Literal:
					return TokenColor.Number;
				case TokenType.Delimiter:
				case TokenType.Operator:
				case TokenType.Text:
				case TokenType.Unknown:
				case TokenType.WhiteSpace:
					return TokenColor.Text;
				case TokenType.String:
					return TokenColor.String;
			}

			return TokenColor.Text;
		}

		private static TokenTriggers GetTriggers( GoTokenID tokenID )
		{
			var result = TokenTriggers.None;
			switch( tokenID )
			{
				case GoTokenID.PERIOD:
					result |= TokenTriggers.MemberSelect;
					break;
				case GoTokenID.LBRACK:
				case GoTokenID.LBRACE:
				case GoTokenID.RBRACK:
				case GoTokenID.RBRACE:
					result |= TokenTriggers.MatchBraces;
					result |= TokenTriggers.MemberSelect;
					break;
				case GoTokenID.LPAREN:
					result |= TokenTriggers.MatchBraces;
					result |= TokenTriggers.ParameterStart;
					result |= TokenTriggers.MemberSelect;
					break;
				case GoTokenID.COMMA:
					return TokenTriggers.ParameterNext;
				case GoTokenID.RPAREN:
					result |= TokenTriggers.MatchBraces;
					result |= TokenTriggers.ParameterEnd;
					result |= TokenTriggers.MemberSelect;
					break;
			}
			return TokenTriggers.None;
		}
	}
}
