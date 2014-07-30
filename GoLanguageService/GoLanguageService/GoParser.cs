using Fitbos.GoLanguageService.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoParser
	{
		private GoSourceFile m_file;

		private GoErrorList m_errors;

		private GoLexer m_lexer;

		private List<GoCommentGroup> m_comments;

		private GoCommentGroup m_leadComment;

		private GoCommentGroup m_lineComment;

		private int m_pos;

		private GoTokenID m_tok;

		private string m_lit;

		private int m_syncPos;

		private int m_syncCnt;

		private int m_exprLev;

		private bool m_inRhs;

		private GoScope m_pkgScope;

		private GoScope m_topScope;

		private List<GoIdent> m_unresolved;

		private List<GoImportSpec> m_imports;

		private GoScope m_labelScope;

		private Stack<List<GoIdent>> m_targetStack;

		private static GoObject s_unresolved = new GoObject();

		private delegate GoSpec ParseSpecFunction( GoCommentGroup doc, GoTokenID keyword, int iota );

		public GoParser( GoSourceFileSet fset, string filename, string text )
		{
			m_errors = new GoErrorList();
			m_tok = GoTokenID.ILLEGAL;
			m_comments = new List<GoCommentGroup>();
			m_unresolved = new List<GoIdent>();
			m_imports = new List<GoImportSpec>();
			m_targetStack = new Stack<List<GoIdent>>();

			m_file = fset.AddFile( filename, -1, text.Length );

			m_lexer = new GoLexer();
			m_lexer.SetSource( m_file, text, 0 );

			this.Next();
		}

		public GoFile ParseFile()
		{
			var doc = m_leadComment;
			var pos = this.Expect( GoTokenID.PACKAGE );

			if( m_errors.Length != 0 )
			{
				return null;
			}

			var ident = this.ParseIdent();
			if( ident.Name == "_" )
			{
				this.Error( m_pos, "Invalid package name _" );
			}
			this.ExpectSemi();

			if( m_errors.Length != 0 )
			{
				return null;
			}

			this.OpenScope();
			m_pkgScope = m_topScope;
			var decls = new List<GoDecl>();
			while( m_tok == GoTokenID.IMPORT )
			{
				decls.Add( this.ParseGenDecl( GoTokenID.IMPORT, this.ParseImportSpec ) );
			}
			while( m_tok != GoTokenID.EOF )
			{
				decls.Add( this.ParseDecl( SyncDecl ) );
			}
			this.CloseScope();
			Assert( m_topScope == null, "Unbalanced scopes" );
			Assert( m_labelScope == null, "Unbalanced label scopes" );

			var i = 0;
			for( int j = 0; j < m_unresolved.Count; j++ )
			{
				var id = m_unresolved[j];
				Assert( id.Obj == s_unresolved, "Object already resolved" );
				id.Obj = m_pkgScope.Lookup( id.Name );
				if( id.Obj == null )
				{
					m_unresolved[i] = id;
					i++;
				}
			}

			var file = new GoFile();
			file.Doc = doc;
			file.Package = pos;
			file.Name = ident;
			file.Decls = decls;
			file.Scope = m_pkgScope;
			file.Imports = m_imports;
			file.Unresolved = m_unresolved.GetRange( 0, i );
			file.Comments = m_comments;
			return file;
		}

		private void Assert( bool cond, string msg )
		{
			throw new NotImplementedException();
		}

		private void CloseScope()
		{
			throw new NotImplementedException();
		}

		private static void SyncDecl( GoParser parser )
		{
			throw new NotImplementedException();
		}

		private GoDecl ParseDecl( Action<GoParser> sync )
		{
			throw new NotImplementedException();
		}

		private GoSpec ParseImportSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
		{
			throw new NotImplementedException();
		}

		private GoGenDecl ParseGenDecl( GoTokenID keyword, ParseSpecFunction f )
		{
			throw new NotImplementedException();
		}

		private void OpenScope()
		{
			throw new NotImplementedException();
		}

		private void ExpectSemi()
		{
			throw new NotImplementedException();
		}

		private GoIdent ParseIdent()
		{
			throw new NotImplementedException();
		}

		private int Expect( GoTokenID goTokenID )
		{
			throw new NotImplementedException();
		}

		private void Next()
		{
			m_leadComment = null;
			m_lineComment = null;
			var prev = m_pos;
			this.Next0();

			if( m_tok == GoTokenID.COMMENT )
			{
				GoCommentGroup comment = null;
				var endline = 0;

				if( m_file.Line( m_pos ) == m_file.Line( prev ) )
				{
					this.ConsumeCommentGroup( out comment, out endline, 0 );
					if( m_file.Line( m_pos ) != endline )
					{
						m_lineComment = comment;
					}
				}

				endline = -1;
				while( m_tok == GoTokenID.COMMENT )
				{
					this.ConsumeCommentGroup( out comment, out endline, 1 );
				}

				if( endline + 1 == m_file.Line( m_pos ) )
				{
					m_leadComment = comment;
				}
			}
		}

		private void ConsumeCommentGroup( out GoCommentGroup comments, out int endline, int n )
		{
			var list = new List<GoComment>();
			endline = m_file.Line( m_pos );
			while( m_tok == GoTokenID.COMMENT && m_file.Line( m_pos ) <= endline + n )
			{
				GoComment comment = null;
				this.ConsumeComment( out comment, out endline );
				list.Add( comment );
			}

			comments = new GoCommentGroup( list );
			m_comments.Add( comments );
		}

		private void ConsumeComment( out GoComment comment, out int endline )
		{
			endline = m_file.Line( m_pos );
			if( m_lit[1] == '*' )
			{
				foreach( var ch in m_lit )
				{
					if( ch == '\n' )
					{
						endline++;
					}
				}
			}

			comment = new GoComment( m_pos, m_lit );
			this.Next0();
		}

		private void Next0()
		{
			// Skip trace for now
			/*
			if p.trace && p.pos.IsValid() {
				s := p.tok.String()
				switch {
				case p.tok.IsLiteral():
					p.printTrace(s, p.lit)
				case p.tok.IsOperator(), p.tok.IsKeyword():
					p.printTrace("\"" + s + "\"")
				default:
					p.printTrace(s)
				}
			}
			*/

			int state = 0;
			var token = m_lexer.GetToken( ref state );
			m_pos = token.Position;
			m_tok = token.ID;
			m_lit = token.Text;
		}

		private void Error( int pos, string msg )
		{
			var epos = m_file.Position( pos );
			m_errors.Add( epos, msg );
		}
	}
}
