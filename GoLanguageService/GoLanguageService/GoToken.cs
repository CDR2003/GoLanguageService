using Microsoft.VisualStudio.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoToken
	{
		public const int LowestPrecedence = 0;

		public const int UnaryPrecedence = 6;

		public const int HighestPrecedence = 7;

		public int Position;

		public GoTokenID ID;

		public string Text;

		private static Dictionary<GoTokenID, string> s_tokens;

		private static Dictionary<string, GoTokenID> s_keywords;

		static GoToken()
		{
			s_tokens = new Dictionary<GoTokenID, string>();

			s_tokens.Add( GoTokenID.ILLEGAL, "ILLEGAL" );

			s_tokens.Add( GoTokenID.EOF, "EOF" );
			s_tokens.Add( GoTokenID.COMMENT, "COMMENT" );

			s_tokens.Add( GoTokenID.IDENT, "IDENT" );
			s_tokens.Add( GoTokenID.INT, "INT" );
			s_tokens.Add( GoTokenID.FLOAT, "FLOAT" );
			s_tokens.Add( GoTokenID.IMAG, "IMAG" );
			s_tokens.Add( GoTokenID.CHAR, "CHAR" );
			s_tokens.Add( GoTokenID.STRING, "STRING" );

			s_tokens.Add( GoTokenID.ADD, "+" );
			s_tokens.Add( GoTokenID.SUB, "-" );
			s_tokens.Add( GoTokenID.MUL, "*" );
			s_tokens.Add( GoTokenID.QUO, "/" );
			s_tokens.Add( GoTokenID.REM, "%" );

			s_tokens.Add( GoTokenID.AND, "&" );
			s_tokens.Add( GoTokenID.OR, "|" );
			s_tokens.Add( GoTokenID.XOR, "^" );
			s_tokens.Add( GoTokenID.SHL, "<<" );
			s_tokens.Add( GoTokenID.SHR, ">>" );
			s_tokens.Add( GoTokenID.AND_NOT, "&^" );

			s_tokens.Add( GoTokenID.ADD_ASSIGN, "+=" );
			s_tokens.Add( GoTokenID.SUB_ASSIGN, "-=" );
			s_tokens.Add( GoTokenID.MUL_ASSIGN, "*=" );
			s_tokens.Add( GoTokenID.QUO_ASSIGN, "/=" );
			s_tokens.Add( GoTokenID.REM_ASSIGN, "%=" );

			s_tokens.Add( GoTokenID.AND_ASSIGN, "&=" );
			s_tokens.Add( GoTokenID.OR_ASSIGN, "|=" );
			s_tokens.Add( GoTokenID.XOR_ASSIGN, "^=" );
			s_tokens.Add( GoTokenID.SHL_ASSIGN, "<<=" );
			s_tokens.Add( GoTokenID.SHR_ASSIGN, ">>=" );
			s_tokens.Add( GoTokenID.AND_NOT_ASSIGN, "&^=" );

			s_tokens.Add( GoTokenID.LAND, "&&" );
			s_tokens.Add( GoTokenID.LOR, "||" );
			s_tokens.Add( GoTokenID.ARROW, "<-" );
			s_tokens.Add( GoTokenID.INC, "++" );
			s_tokens.Add( GoTokenID.DEC, "--" );

			s_tokens.Add( GoTokenID.EQL, "==" );
			s_tokens.Add( GoTokenID.LSS, "<" );
			s_tokens.Add( GoTokenID.GTR, ">" );
			s_tokens.Add( GoTokenID.ASSIGN, "=" );
			s_tokens.Add( GoTokenID.NOT, "!" );

			s_tokens.Add( GoTokenID.NEQ, "!=" );
			s_tokens.Add( GoTokenID.LEQ, "<=" );
			s_tokens.Add( GoTokenID.GEQ, ">=" );
			s_tokens.Add( GoTokenID.DEFINE, ":=" );
			s_tokens.Add( GoTokenID.ELLIPSIS, "..." );

			s_tokens.Add( GoTokenID.LPAREN, "(" );
			s_tokens.Add( GoTokenID.LBRACK, "[" );
			s_tokens.Add( GoTokenID.LBRACE, "{" );
			s_tokens.Add( GoTokenID.COMMA, "," );
			s_tokens.Add( GoTokenID.PERIOD, "." );

			s_tokens.Add( GoTokenID.RPAREN, ")" );
			s_tokens.Add( GoTokenID.RBRACK, "]" );
			s_tokens.Add( GoTokenID.RBRACE, "}" );
			s_tokens.Add( GoTokenID.SEMICOLON, ";" );
			s_tokens.Add( GoTokenID.COLON, ":" );

			s_tokens.Add( GoTokenID.BREAK, "break" );
			s_tokens.Add( GoTokenID.CASE, "case" );
			s_tokens.Add( GoTokenID.CHAN, "chan" );
			s_tokens.Add( GoTokenID.CONST, "const" );
			s_tokens.Add( GoTokenID.CONTINUE, "continue" );

			s_tokens.Add( GoTokenID.DEFAULT, "default" );
			s_tokens.Add( GoTokenID.DEFER, "defer" );
			s_tokens.Add( GoTokenID.ELSE, "else" );
			s_tokens.Add( GoTokenID.FALLTHROUGH, "fallthrough" );
			s_tokens.Add( GoTokenID.FOR, "for" );

			s_tokens.Add( GoTokenID.FUNC, "func" );
			s_tokens.Add( GoTokenID.GO, "go" );
			s_tokens.Add( GoTokenID.GOTO, "goto" );
			s_tokens.Add( GoTokenID.IF, "if" );
			s_tokens.Add( GoTokenID.IMPORT, "import" );

			s_tokens.Add( GoTokenID.INTERFACE, "interface" );
			s_tokens.Add( GoTokenID.MAP, "map" );
			s_tokens.Add( GoTokenID.PACKAGE, "package" );
			s_tokens.Add( GoTokenID.RANGE, "range" );
			s_tokens.Add( GoTokenID.RETURN, "return" );

			s_tokens.Add( GoTokenID.SELECT, "select" );
			s_tokens.Add( GoTokenID.STRUCT, "struct" );
			s_tokens.Add( GoTokenID.SWITCH, "switch" );
			s_tokens.Add( GoTokenID.TYPE, "type" );
			s_tokens.Add( GoTokenID.VAR, "var" );

			s_keywords = new Dictionary<string, GoTokenID>();
			for( var i = GoTokenID.keyword_beg + 1; i < GoTokenID.keyword_end; i++ )
			{
				s_keywords.Add( s_tokens[i], i );
			}
		}

		public static GoTokenID Lookup( string id )
		{
			GoTokenID tok;
			if( s_keywords.TryGetValue( id, out tok ) )
			{
				return tok;
			}
			return GoTokenID.IDENT;
		}

		public static int GetPrecedence( GoTokenID op )
		{
			switch( op )
			{
				case GoTokenID.LOR:
					return 1;
				case GoTokenID.LAND:
					return 2;
				case GoTokenID.EQL:
				case GoTokenID.NEQ:
				case GoTokenID.LSS:
				case GoTokenID.LEQ:
				case GoTokenID.GTR:
				case GoTokenID.GEQ:
					return 3;
				case GoTokenID.ADD:
				case GoTokenID.SUB:
				case GoTokenID.OR:
				case GoTokenID.XOR:
					return 4;
				case GoTokenID.MUL:
				case GoTokenID.QUO:
				case GoTokenID.REM:
				case GoTokenID.SHL:
				case GoTokenID.SHR:
				case GoTokenID.AND:
				case GoTokenID.AND_NOT:
					return 5;
			}
			return LowestPrecedence;
		}

		public static bool IsLiteral( GoTokenID tok )
		{
			return GoTokenID.literal_beg < tok && tok < GoTokenID.literal_end;
		}

		public static bool IsOperator( GoTokenID tok )
		{
			return GoTokenID.operator_beg < tok && tok < GoTokenID.operator_end;
		}

		public static bool IsKeyword( GoTokenID tok )
		{
			return GoTokenID.keyword_beg < tok && tok < GoTokenID.keyword_end;
		}

		public static string ToString( GoTokenID tok )
		{
			var s = "";
			if( 0 <= tok && tok < (GoTokenID)s_tokens.Count )
			{
				s = s_tokens[tok];
			}
			if( s == "" )
			{
				s = "token(" + (int)tok + ")";
			}
			return s;
		}
	}
}
