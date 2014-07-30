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
		private enum SimpleStmtParseMode
		{
			Basic,
			LabelOk,
			RangeOk,
		}

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

		private List<List<GoIdent>> m_targetStack;

		private static GoObject s_unresolved = new GoObject();

		private delegate GoSpec ParseSpecFunction( GoCommentGroup doc, GoTokenID keyword, int iota );

		public GoParser( GoSourceFileSet fset, string filename, string text )
		{
			m_errors = new GoErrorList();
			m_tok = GoTokenID.ILLEGAL;
			m_comments = new List<GoCommentGroup>();
			m_unresolved = new List<GoIdent>();
			m_imports = new List<GoImportSpec>();
			m_targetStack = new List<List<GoIdent>>();

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
			if( cond == false )
			{
				throw new Exception( "GoParser internal error: " + msg );
			}
		}

		private void OpenScope()
		{
			m_topScope = new GoScope( m_topScope );
		}

		private void CloseScope()
		{
			m_topScope = m_topScope.Outer;
		}

		private static void SyncDecl( GoParser p )
		{
			for( ; ; )
			{
				switch( p.m_tok )
				{
					case GoTokenID.CONST:
					case GoTokenID.TYPE:
					case GoTokenID.VAR:
						if( p.m_pos == p.m_syncPos && p.m_syncCnt < 10 )
						{
							p.m_syncCnt++;
							return;
						}
						if( p.m_pos > p.m_syncPos )
						{
							p.m_syncPos = p.m_pos;
							p.m_syncCnt = 0;
							return;
						}
						break;
					case GoTokenID.EOF:
						return;
				}
				p.Next();
			}
		}

		private static void SyncStmt( GoParser p )
		{
			for( ; ; )
			{
				switch( p.m_tok )
				{
					case GoTokenID.BREAK:
					case GoTokenID.CONST:
					case GoTokenID.CONTINUE:
					case GoTokenID.DEFER:
					case GoTokenID.FALLTHROUGH:
					case GoTokenID.FOR:
					case GoTokenID.GO:
					case GoTokenID.GOTO:
					case GoTokenID.IF:
					case GoTokenID.RETURN:
					case GoTokenID.SELECT:
					case GoTokenID.SWITCH:
					case GoTokenID.TYPE:
					case GoTokenID.VAR:
						if( p.m_pos == p.m_syncPos && p.m_syncCnt < 10 )
						{
							p.m_syncCnt++;
							return;
						}
						if( p.m_pos > p.m_syncPos )
						{
							p.m_syncPos = p.m_pos;
							p.m_syncCnt = 0;
							return;
						}
						break;
					case GoTokenID.EOF:
						return;
				}
				p.Next();
			}
		}

		private GoDecl ParseDecl( Action<GoParser> sync )
		{
			ParseSpecFunction f;
			switch( m_tok )
			{
				case GoTokenID.CONST:
				case GoTokenID.VAR:
					f = this.ParseValueSpec;
					break;
				case GoTokenID.TYPE:
					f = this.ParseTypeSpec;
					break;
				case GoTokenID.FUNC:
					return this.ParseFuncDecl();
				default:
					{
						var pos = m_pos;
						this.ErrorExpected( pos, "declaration" );
						sync( this );
						return new GoBadDecl { From = pos, To = m_pos };
					}
			}

			return this.ParseGenDecl( m_tok, f );
		}

		private void ErrorExpected( int pos, string msg )
		{
			msg = "Expected " + msg;
			if( pos == m_pos )
			{
				if( m_tok == GoTokenID.SEMICOLON && m_lit == "\n" )
				{
					msg += ", found newline";
				}
				else
				{
					msg += ", found '" + GoToken.ToString( m_tok ) + "'";
					if( GoToken.IsLiteral( m_tok ) )
					{
						msg += " " + m_lit;
					}
				}
			}
			this.Error( pos, msg );
		}

		private GoDecl ParseFuncDecl()
		{
			var doc = m_leadComment;
			var pos = this.Expect( GoTokenID.FUNC );
			var scope = new GoScope( m_topScope );

			GoFieldList recv = null;
			if( m_tok == GoTokenID.LPAREN )
			{
				recv = this.ParseReceiver( scope );
			}

			var ident = this.ParseIdent();

			GoFieldList parameters = null;
			GoFieldList results = null;
			this.ParseSignature( out parameters, out results, scope );

			GoBlockStmt body = null;
			if( m_tok == GoTokenID.LBRACE )
			{
				body = this.ParseBody( scope );
			}
			this.ExpectSemi();

			var decl = new GoFuncDecl();
			decl.Doc = doc;
			decl.Recv = recv;
			decl.Name = ident;
			decl.Type = new GoFuncType { Func = pos, Params = parameters, Results = results };
			decl.Body = body;
			if( recv == null )
			{
				if( ident.Name != "init" )
				{
					this.Declare( decl, null, m_pkgScope, GoObjectKind.Fun, ident );
				}
			}

			return decl;
		}

		private void Declare( object decl, object data, GoScope scope, GoObjectKind kind, params GoIdent[] idents )
		{
			foreach( var ident in idents )
			{
				this.Assert( ident.Obj == null, "Identifier already declared or resolved" );
				var obj = new GoObject( kind, ident.Name );
				obj.Decl = decl;
				obj.Data = data;
				ident.Obj = obj;
				if( ident.Name != "_" )
				{
					var alt = scope.Insert( obj );
					if( alt != null )
					{
						var prevDecl = "";
						var pos = alt.Pos;
						if( pos != 0 )
						{
							prevDecl = "\n\tPrevious declaration at " + m_file.Position( pos );
						}
						this.Error( ident.Pos, ident.Name + " redeclared in this block" + prevDecl );
					}
				}
			}
		}

		private GoBlockStmt ParseBody( GoScope scope )
		{
			var lbrace = this.Expect( GoTokenID.LBRACE );
			m_topScope = scope;
			this.OpenLabelScope();
			var list = this.ParseStmtList();
			this.CloseLabelScope();
			this.CloseScope();
			var rbrace = this.Expect( GoTokenID.RBRACE );

			return new GoBlockStmt { Lbrace = lbrace, List = list, Rbrace = rbrace };
		}

		private void CloseLabelScope()
		{
			var n = m_targetStack.Count - 1;
			var scope = m_labelScope;
			foreach( var ident in m_targetStack[n] )
			{
				ident.Obj = scope.Lookup( ident.Name );
				if( ident.Obj == null )
				{
					this.Error( ident.Pos, "Label " + ident.Name + " undefined" );
				}
			}

			m_targetStack.RemoveAt( n );
			m_labelScope = m_labelScope.Outer;
		}

		private List<GoStmt> ParseStmtList()
		{
			var list = new List<GoStmt>();
			while( m_tok != GoTokenID.CASE && m_tok != GoTokenID.DEFAULT && m_tok != GoTokenID.RBRACE && m_tok != GoTokenID.EOF )
			{
				list.Add( this.ParseStmt() );
			}
			return list;
		}

		private GoStmt ParseStmt()
		{
			GoStmt s = null;
			switch( m_tok )
			{
				case GoTokenID.CONST:
				case GoTokenID.TYPE:
				case GoTokenID.VAR:
					s = new GoDeclStmt { Decl = this.ParseDecl( SyncStmt ) };
					break;
				case GoTokenID.IDENT:
				case GoTokenID.INT:
				case GoTokenID.FLOAT:
				case GoTokenID.IMAG:
				case GoTokenID.CHAR:
				case GoTokenID.STRING:
				case GoTokenID.FUNC:
				case GoTokenID.LPAREN:
				case GoTokenID.LBRACK:
				case GoTokenID.STRUCT:
				case GoTokenID.ADD:
				case GoTokenID.SUB:
				case GoTokenID.MUL:
				case GoTokenID.AND:
				case GoTokenID.XOR:
				case GoTokenID.ARROW:
				case GoTokenID.NOT:
					this.ParseSimpleStmt( out s, SimpleStmtParseMode.LabelOk );
					if( s is GoLabeledStmt == false )
					{
						this.ExpectSemi();
					}
					break;
				case GoTokenID.GO:
					s = this.ParseGoStmt();
					break;
				case GoTokenID.DEFER:
					s = this.ParseDeferStmt();
					break;
				case GoTokenID.RETURN:
					s = this.ParseReturnStmt();
					break;
				case GoTokenID.BREAK:
				case GoTokenID.CONTINUE:
				case GoTokenID.GOTO:
				case GoTokenID.FALLTHROUGH:
					s = this.ParseBranchStmt( m_tok );
					break;
				case GoTokenID.LBRACE:
					s = this.ParseBlockStmt();
					this.ExpectSemi();
					break;
				case GoTokenID.IF:
					s = this.ParseIfStmt();
					break;
				case GoTokenID.SWITCH:
					s = this.ParseSwitchStmt();
					break;
				case GoTokenID.SELECT:
					s = this.ParseSelectStmt();
					break;
				case GoTokenID.FOR:
					s = this.ParseForStmt();
					break;
				case GoTokenID.SEMICOLON:
					s = new GoEmptyStmt { Semicolon = m_pos };
					this.Next();
					break;
				case GoTokenID.RBRACE:
					s = new GoEmptyStmt { Semicolon = m_pos };
					break;
				default:
					{
						var pos = m_pos;
						this.ErrorExpected( pos, "statement" );
						SyncStmt( this );
						s = new GoBadStmt { From = pos, To = m_pos };
					}
					break;
			}

			return s;
		}

		private GoStmt ParseForStmt()
		{
			var pos = this.Expect( GoTokenID.FOR );
			this.OpenScope();

			GoStmt s1 = null, s2 = null, s3 = null;
			bool isRange = false;
			if( m_tok != GoTokenID.LBRACE )
			{
				var prevLev = m_exprLev;
				m_exprLev = -1;
				if( m_tok != GoTokenID.SEMICOLON )
				{
					isRange = this.ParseSimpleStmt( out s2, SimpleStmtParseMode.RangeOk );
				}
				if( isRange == false && m_tok == GoTokenID.SEMICOLON )
				{
					this.Next();
					s1 = s2;
					s2 = null;
					if( m_tok != GoTokenID.SEMICOLON )
					{
						this.ParseSimpleStmt( out s2, SimpleStmtParseMode.Basic );
					}
					this.ExpectSemi();
					if( m_tok != GoTokenID.LBRACE )
					{
						this.ParseSimpleStmt( out s3, SimpleStmtParseMode.Basic );
					}
				}
				m_exprLev = prevLev;
			}

			var body = this.ParseBlockStmt();
			this.ExpectSemi();

			if( isRange )
			{
				var assign = s2 as GoAssignStmt;
				GoExpr key = null, value = null;
				switch( assign.Lhs.Count )
				{
					case 2:
						key = assign.Lhs[0];
						value = assign.Lhs[1];
						break;
					case 1:
						key = assign.Lhs[0];
						break;
					default:
						this.ErrorExpected( assign.Lhs[0].Pos, "1 or 2 expressions" );
						this.CloseScope();
						return new GoBadStmt { From = pos, To = this.SafePos( body.End ) };
				}

				var x = ( assign.Rhs[0] as GoUnaryExpr ).X;
				this.CloseScope();

				var rangeStmt = new GoRangeStmt();
				rangeStmt.For = pos;
				rangeStmt.Key = key;
				rangeStmt.Value = value;
				rangeStmt.TokPos = assign.TokPos;
				rangeStmt.Tok = assign.Tok;
				rangeStmt.X = x;
				rangeStmt.Body = body;
				return rangeStmt;
			}

			this.CloseScope();

			var forStmt = new GoForStmt();
			forStmt.For = pos;
			forStmt.Init = s1;
			forStmt.Cond = this.MakeExpr( s2, "boolean or range expression" );
			forStmt.Post = s3;
			forStmt.Body = body;
			return forStmt;
		}

		private GoExpr MakeExpr( GoStmt s, string kind )
		{
			if( s == null )
			{
				return null;
			}
			var es = s as GoExprStmt;
			if( es != null )
			{
				return this.CheckExpr( es.X );
			}
			this.Error( s.Pos, "Expected " + kind + ", found simple statement (missing parentheses around composite literal?)" );
			return new GoBadExpr { From = s.Pos, To = this.SafePos( s.End ) };
		}

		private GoExpr CheckExpr( GoExpr x )
		{
			var unparened = Unparen( x );
			if( unparened is GoBadExpr ||
				unparened is GoIdent ||
				unparened is GoBasicLit ||
				unparened is GoFuncLit ||
				unparened is GoCompositeLit )
			{
			}
			else if( unparened is GoParenExpr )
			{
				throw new Exception( "Unreachable" );
			}
			else if( x is GoSelectorExpr ||
				x is GoIndexExpr ||
				x is GoSliceExpr ||
				x is GoTypeAssertExpr ||
				x is GoCallExpr ||
				x is GoStarExpr ||
				x is GoUnaryExpr ||
				x is GoBinaryExpr )
			{
			}
			else
			{
				this.ErrorExpected( x.Pos, "expression" );
				x = new GoBadExpr { From = x.Pos, To = this.SafePos( x.End ) };
			}
			return x;
		}

		private static GoExpr Unparen( GoExpr x )
		{
			if( x is GoParenExpr )
			{
				var p = x as GoParenExpr;
				x = Unparen( p.X );
			}
			return x;
		}

		private int SafePos( int pos )
		{
			int res = 0;
			try
			{
				m_file.Offset( pos );
			}
			catch( Exception )
			{
				res = m_file.Base + m_file.Size;
			}
			return res;
		}

		private GoSelectStmt ParseSelectStmt()
		{
			var pos = this.Expect( GoTokenID.SELECT );
			var lbrace = this.Expect( GoTokenID.LBRACE );
			var list = new List<GoStmt>();
			while( m_tok == GoTokenID.CASE || m_tok == GoTokenID.DEFAULT )
			{
				list.Add( this.ParseCommClause() );
			}
			var rbrace = this.Expect( GoTokenID.RBRACE );
			this.ExpectSemi();
			var body = new GoBlockStmt { Lbrace = lbrace, List = list, Rbrace = rbrace };

			return new GoSelectStmt { Select = pos, Body = body };
		}

		private GoCommClause ParseCommClause()
		{
			this.OpenScope();
			var pos = m_pos;
			GoStmt comm = null;
			if( m_tok == GoTokenID.CASE )
			{
				this.Next();
				var lhs = this.ParseLhsList();
				if( m_tok == GoTokenID.ARROW )
				{
					if( lhs.Count > 1 )
					{
						this.ErrorExpected( lhs[0].Pos, "1 expression" );
					}
					var arrow = m_pos;
					this.Next();
					var rhs = this.ParseRhs();
					comm = new GoSendStmt { Chan = lhs[0], Arrow = arrow, Value = rhs };
				}
				else
				{
					var tok = m_tok;
					if( tok == GoTokenID.ASSIGN || tok == GoTokenID.DEFINE )
					{
						if( lhs.Count > 2 )
						{
							this.ErrorExpected( lhs[0].Pos, "1 or 2 expressions" );
							lhs = lhs.GetRange( 0, 2 );
						}
						var p = m_pos;
						this.Next();
						var rhs = this.ParseRhs();
						var assign = new GoAssignStmt { Lhs = lhs, TokPos = p, Tok = tok, Rhs = new List<GoExpr> { rhs } };
						if( tok == GoTokenID.DEFINE )
						{
							this.ShortVarDecl( assign, lhs );
						}
						comm = assign;
					}
					else
					{
						if( lhs.Count > 1 )
						{
							this.ErrorExpected( lhs[0].Pos, "1 expression" );
						}
						comm = new GoExprStmt { X = lhs[0] };
					}
				}
			}
			else
			{
				this.Expect( GoTokenID.DEFAULT );
			}

			var colon = this.Expect( GoTokenID.COLON );
			var body = this.ParseStmtList();
			this.CloseScope();

			return new GoCommClause { Case = pos, Comm = comm, Colon = colon, Body = body };
		}

		private void ShortVarDecl( GoAssignStmt decl, List<GoExpr> list )
		{
			var n = 0;
			foreach( var x in list )
			{
				var ident = x as GoIdent;
				if( ident != null )
				{
					this.Assert( ident.Obj == null, "Identifier already declared or resolved" );
					var obj = new GoObject( GoObjectKind.Var, ident.Name );
					obj.Decl = decl;
					ident.Obj = obj;
					if( ident.Name != "_" )
					{
						var alt = m_topScope.Insert( obj );
						if( alt != null )
						{
							ident.Obj = alt;
						}
						else
						{
							n++;
						}
					}
				}
				else
				{
					this.ErrorExpected( x.Pos, "identifier on left side of :=" );
				}
			}
			if( n == 0 )
			{
				this.Error( list[0].Pos, "No new variables on left side of :=" );
			}
		}

		private GoExpr ParseRhs()
		{
			var old = m_inRhs;
			m_inRhs = true;
			var x = this.CheckExpr( this.ParseExpr( false ) );
			m_inRhs = old;
			return x;
		}

		private GoExpr ParseExpr( bool lhs )
		{
			return this.ParseBinaryExpr( lhs, GoToken.LowestPrecedence + 1 );
		}

		private GoExpr ParseBinaryExpr( bool lhs, int prec1 )
		{
			var x = this.ParseUnaryExpr( lhs );
			int prec = 0;
			for( this.TokPrec( out prec ); prec >= prec1; prec-- )
			{
				for( ; ; )
				{
					int oprec = 0;
					GoTokenID op = this.TokPrec( out oprec );
					if( oprec != prec )
					{
						break;
					}
					var pos = this.Expect( op );
					if( lhs )
					{
						this.Resolve( x );
						lhs = false;
					}
					var y = this.ParseBinaryExpr( false, prec + 1 );
					x = new GoBinaryExpr { X = this.CheckExpr( x ), OpPos = pos, Op = op, Y = this.CheckExpr( y ) };
				}
			}

			return x;
		}

		private void Resolve( GoExpr x )
		{
			this.TryResolve( x, true );
		}

		private void TryResolve( GoExpr x, bool collectUnresolved )
		{
			var ident = x as GoIdent;
			if( ident == null )
			{
				return;
			}
			this.Assert( ident.Obj == null, "Identifier already declared or resolved" );
			if( ident.Name == "_" )
			{
				return;
			}

			for( var s = m_topScope; s != null; s = s.Outer )
			{
				var obj = s.Lookup( ident.Name );
				if( obj != null )
				{
					ident.Obj = obj;
					return;
				}
			}

			if( collectUnresolved )
			{
				ident.Obj = s_unresolved;
				m_unresolved.Add( ident );
			}
		}

		private GoTokenID TokPrec( out int prec )
		{
			var tok = m_tok;
			if( m_inRhs && tok == GoTokenID.ASSIGN )
			{
				tok = GoTokenID.EQL;
			}
			prec = GoToken.GetPrecedence( tok );
			return tok;
		}

		private GoExpr ParseUnaryExpr( bool lhs )
		{
			switch( m_tok )
			{
				case GoTokenID.ADD:
				case GoTokenID.SUB:
				case GoTokenID.NOT:
				case GoTokenID.XOR:
				case GoTokenID.AND:
					{
						var pos = m_pos;
						var op = m_tok;
						this.Next();
						var x = this.ParseUnaryExpr( false );
						return new GoUnaryExpr { OpPos = pos, Op = op, X = this.CheckExpr( x ) };
					}
				case GoTokenID.ARROW:
					{
						var arrow = m_pos;
						this.Next();

						var x = this.ParseUnaryExpr( false );

						var ok = x is GoChanType;
						var type = x as GoChanType;
						if( type != null )
						{
							var dir = GoChanDir.Send;
							while( ok && dir == GoChanDir.Send )
							{
								if( type.Dir == GoChanDir.Recv )
								{
									this.ErrorExpected( type.Arrow, "'chan'" );
								}
								type.Begin = arrow;
								var swapper = arrow;
								arrow = type.Arrow;
								type.Arrow = swapper;

								dir = type.Dir;
								type.Dir = GoChanDir.Recv;

								ok = type.Value is GoChanType;
								type = type.Value as GoChanType;
							}
							if( dir == GoChanDir.Send )
							{
								this.ErrorExpected( arrow, "channel type" );
							}

							return x;
						}

						return new GoUnaryExpr { OpPos = arrow, Op = GoTokenID.ARROW, X = this.CheckExpr( x ) };
					}

				case GoTokenID.MUL:
					{
						var pos = m_pos;
						this.Next();
						var x = this.ParseUnaryExpr( false );
						return new GoStarExpr { Star = pos, X = this.CheckExprOrType( x ) };
					}
			}

			return this.ParsePrimaryExpr( lhs );
		}

		private GoExpr ParsePrimaryExpr( bool lhs )
		{
			var x = this.ParseOperand( lhs );

			for( ; ; )
			{
				switch( m_tok )
				{
					case GoTokenID.PERIOD:
						this.Next();
						if( lhs )
						{
							this.Resolve( x );
						}
						switch( m_tok )
						{
							case GoTokenID.IDENT:
								x = this.ParseSelector( this.CheckExprOrType( x ) );
								break;
							case GoTokenID.LPAREN:
								x = this.ParseTypeAssertion( this.CheckExpr( x ) );
								break;
							default:
								{
									var pos = m_pos;
									this.ErrorExpected( pos, "selector or type assertion" );
									this.Next();
									x = new GoBadExpr { From = pos, To = m_pos };
								}
								break;
						}
						break;
					case GoTokenID.LBRACK:
						if( lhs )
						{
							this.Resolve( x );
						}
						x = this.ParseIndexOrSlice( this.CheckExpr( x ) );
						break;
					case GoTokenID.LPAREN:
						if( lhs )
						{
							this.Resolve( x );
						}
						x = this.ParseCallOrConversion( this.CheckExprOrType( x ) );
						break;
					case GoTokenID.LBRACE:
						if( IsLiteralType( x ) && ( m_exprLev >= 0 || IsTypeName( x ) == false ) )
						{
							if( lhs )
							{
								this.Resolve( x );
							}
							x = this.ParseLiteralValue( x );
						}
						else
						{
							goto exit;
						}
						break;
					default:
						goto exit;
				}
				lhs = false;
			}

		exit:
			return x;
		}

		private GoExpr ParseLiteralValue( GoExpr type )
		{
			var lbrace = this.Expect( GoTokenID.LBRACE );
			var elts = new List<GoExpr>();
			m_exprLev++;
			if( m_tok != GoTokenID.RBRACE )
			{
				elts = this.ParseElementList();
			}
			m_exprLev--;
			var rbrace = this.ExpectClosing( GoTokenID.RBRACE, "composite literal" );
			return new GoCompositeLit { Type = type, Lbrace = lbrace, Elts = elts, Rbrace = rbrace };
		}

		private int ExpectClosing( GoTokenID tok, string context )
		{
			if( m_tok != tok && m_tok == GoTokenID.SEMICOLON && m_lit == "\n" )
			{
				this.Error( m_pos, "Missing ',' before newline in " + context );
				this.Next();
			}
			return this.Expect( tok );
		}

		private List<GoExpr> ParseElementList()
		{
			var list = new List<GoExpr>();
			while( m_tok != GoTokenID.RBRACE && m_tok != GoTokenID.EOF )
			{
				list.Add( this.ParseElement( true ) );
				if( this.AtComma( "composite literal" ) == false )
				{
					break;
				}
			}
			return list;
		}

		private bool AtComma( string context )
		{
			if( m_tok == GoTokenID.COMMA )
			{
				return true;
			}
			if( m_tok == GoTokenID.SEMICOLON && m_lit == "\n" )
			{
				this.Error( m_pos, "Missing ',' before newline in " + context );
				return true;
			}
			return false;
		}

		private GoExpr ParseElement( bool keyOk )
		{
			if( m_tok == GoTokenID.LBRACE )
			{
				return this.ParseLiteralValue( null );
			}

			var x = this.CheckExpr( this.ParseExpr( keyOk ) );
			if( keyOk )
			{
				if( m_tok == GoTokenID.COLON )
				{
					var colon = m_pos;
					this.Next();
					this.TryResolve( x, false );
					return new GoKeyValueExpr { Key = x, Colon = colon, Value = this.ParseElement( false ) };
				}
				this.Resolve( x );
			}

			return x;
		}

		private static bool IsTypeName( GoExpr x )
		{
			if( x is GoBadExpr || x is GoIdent )
			{
				return true;
			}
			else if( x is GoSelectorExpr )
			{
				var t = x as GoSelectorExpr;
				return t.X is GoIdent;
			}
			else
			{
				return false;
			}
		}

		private static bool IsLiteralType( GoExpr x )
		{
			if( x is GoBadExpr ||
				x is GoIdent ||
				x is GoArrayType ||
				x is GoStructType ||
				x is GoMapType )
			{
				return true;
			}
			else if( x is GoSelectorExpr )
			{
				var t = x as GoSelectorExpr;
				return t.X is GoIdent;
			}
			else
			{
				return false;
			}
		}

		private GoCallExpr ParseCallOrConversion( GoExpr fun )
		{
			var lparen = this.Expect( GoTokenID.LPAREN );
			m_exprLev++;
			var list = new List<GoExpr>();
			var ellipsis = 0;
			while( m_tok != GoTokenID.RPAREN && m_tok != GoTokenID.EOF && ellipsis == 0 )
			{
				list.Add( this.ParseRhsOrType() );
				if( m_tok == GoTokenID.ELLIPSIS )
				{
					ellipsis = m_pos;
					this.Next();
				}
				if( this.AtComma( "argument list" ) == false )
				{
					break;
				}
				this.Next();
			}
			m_exprLev--;
			var rparen = this.ExpectClosing( GoTokenID.RPAREN, "argument list" );

			return new GoCallExpr { Fun = fun, Lparen = lparen, Args = list, Ellipsis = ellipsis, Rparen = rparen };
		}

		private GoExpr ParseRhsOrType()
		{
			var old = m_inRhs;
			m_inRhs = true;
			var x = this.CheckExprOrType( this.ParseExpr( false ) );
			m_inRhs = old;
			return x;
		}

		private GoExpr ParseIndexOrSlice( GoExpr x )
		{
			const int N = 3;
			var lbrack = this.Expect( GoTokenID.LBRACK );
			m_exprLev++;
			var index = new GoExpr[N];
			var colons = new int[N - 1];
			if( m_tok != GoTokenID.COLON )
			{
				index[0] = this.ParseRhs();
			}
			var ncolons = 0;
			while( m_tok == GoTokenID.COLON && ncolons < colons.Length )
			{
				colons[ncolons] = m_pos;
				ncolons++;
				this.Next();
				if( m_tok != GoTokenID.COLON && m_tok != GoTokenID.RBRACK && m_tok != GoTokenID.EOF )
				{
					index[ncolons] = this.ParseRhs();
				}
			}
			m_exprLev--;
			var rbrack = this.Expect( GoTokenID.RBRACK );

			if( ncolons > 0 )
			{
				var slice3 = false;
				if( ncolons == 2 )
				{
					slice3 = true;
					if( index[1] == null )
					{
						this.Error( colons[0], "2nd index required in 3-index slice" );
						index[1] = new GoBadExpr { From = colons[0] + 1, To = colons[1] };
					}
					if( index[2] == null )
					{
						this.Error( colons[1], "3rd index required in 3-index slice" );
						index[2] = new GoBadExpr { From = colons[1] + 1, To = rbrack };
					}
				}
				return new GoSliceExpr { X = x, Lbrack = lbrack, Low = index[0], High = index[1], Max = index[2], Slice3 = slice3, Rbrack = rbrack };
			}

			return new GoIndexExpr { X = x, Lbrack = lbrack, Index = index[0], Rbrack = rbrack };
		}

		private GoExpr ParseTypeAssertion( GoExpr x )
		{
			var lparen = this.Expect( GoTokenID.LPAREN );
			GoExpr type = null;
			if( m_tok == GoTokenID.TYPE )
			{
				this.Next();
			}
			else
			{
				type = this.ParseType();
			}
			var rparen = this.Expect( GoTokenID.RPAREN );

			return new GoTypeAssertExpr { X = x, Type = type, Lparen = lparen, Rparen = rparen };
		}

		private GoExpr ParseType()
		{
			var type = this.TryType();

			if( type == null )
			{
				var pos = m_pos;
				this.ErrorExpected( pos, "type" );
				this.Next();
				return new GoBadExpr { From = pos, To = m_pos };
			}

			return type;
		}

		private GoExpr TryType()
		{
			var type = this.TryIdentOrType();
			if( type != null )
			{
				this.Resolve( type );
			}
			return type;
		}

		private GoExpr TryIdentOrType()
		{
			switch( m_tok )
			{
				case GoTokenID.IDENT:
					return this.ParseTypeName();
				case GoTokenID.LBRACK:
					return this.ParseArrayType();
				case GoTokenID.STRUCT:
					return this.ParseStructType();
				case GoTokenID.MUL:
					return this.ParsePointerType();
				case GoTokenID.FUNC:
					{
						GoScope scope = null;
						return this.ParseFuncType( out scope );
					}
				case GoTokenID.INTERFACE:
					return this.ParseInterfaceType();
				case GoTokenID.MAP:
					return this.ParseMapType();
				case GoTokenID.CHAN:
				case GoTokenID.ARROW:
					return this.ParseChanType();
				case GoTokenID.LPAREN:
					{
						var lparen = m_pos;
						this.Next();
						var type = this.ParseType();
						var rparen = this.Expect( GoTokenID.RPAREN );
						return new GoParenExpr { Lparen = lparen, X = type, Rparen = rparen };
					}
			}

			return null;
		}

		private GoExpr ParseChanType()
		{
			var pos = m_pos;
			var dir = GoChanDir.Send | GoChanDir.Recv;
			var arrow = 0;
			if( m_tok == GoTokenID.CHAN )
			{
				this.Next();
				if( m_tok == GoTokenID.ARROW )
				{
					arrow = m_pos;
					this.Next();
					dir = GoChanDir.Send;
				}
			}
			else
			{
				arrow = this.Expect( GoTokenID.ARROW );
				this.Expect( GoTokenID.CHAN );
				dir = GoChanDir.Recv;
			}
			var value = this.ParseType();

			return new GoChanType { Begin = pos, Arrow = arrow, Dir = dir, Value = value };
		}

		private GoExpr ParseMapType()
		{
			var pos = this.Expect( GoTokenID.MAP );
			this.Expect( GoTokenID.LBRACK );
			var key = this.ParseType();
			this.Expect( GoTokenID.RBRACK );
			var value = this.ParseType();

			return new GoMapType { Map = pos, Key = key, Value = value };
		}

		private GoExpr ParseInterfaceType()
		{
			var pos = this.Expect( GoTokenID.INTERFACE );
			var lbrace = this.Expect( GoTokenID.LBRACE );
			var scope = new GoScope( null );
			var list = new List<GoField>();
			while( m_tok == GoTokenID.IDENT )
			{
				list.Add( this.ParseMethodSpec( scope ) );
			}
			var rbrace = this.Expect( GoTokenID.RBRACE );

			var methods = new GoFieldList { Opening = lbrace, List = list, Closing = rbrace };
			return new GoInterfaceType { Interface = pos, Methods = methods };
		}

		private GoField ParseMethodSpec( GoScope scope )
		{
			var doc = m_leadComment;
			var idents = new List<GoIdent>();
			GoExpr type = null;
			var x = this.ParseTypeName();
			if( x is GoIdent && m_tok == GoTokenID.LPAREN )
			{
				var ident = x as GoIdent;
				idents.Add( ident );
				var s = new GoScope( null );
				GoFieldList parameters = null, results = null;
				this.ParseSignature( out parameters, out results, s );
				type = new GoFuncType { Func = 0, Params = parameters, Results = results };
			}
			else
			{
				type = x;
				this.Resolve( type );
			}
			this.ExpectSemi();

			var spec = new GoField { Doc = doc, Names = idents, Type = type, Comment = m_lineComment };
			this.Declare( spec, null, scope, GoObjectKind.Fun, idents.ToArray() );

			return spec;
		}

		private GoFuncType ParseFuncType( out GoScope scope )
		{
			var pos = this.Expect( GoTokenID.FUNC );
			scope = new GoScope( m_topScope );
			GoFieldList parameters = null, results = null;
			this.ParseSignature( out parameters, out results, scope );

			return new GoFuncType { Func = pos, Params = parameters, Results = results };
		}

		private GoStarExpr ParsePointerType()
		{
			var star = this.Expect( GoTokenID.MUL );
			var b = this.ParseType();

			return new GoStarExpr { Star = star, X = b };
		}

		private GoStructType ParseStructType()
		{
			var pos = this.Expect( GoTokenID.STRUCT );
			var lbrace = this.Expect( GoTokenID.LBRACE );
			var scope = new GoScope( null );
			var list = new List<GoField>();
			while( m_tok == GoTokenID.IDENT || m_tok == GoTokenID.MUL || m_tok == GoTokenID.LPAREN )
			{
				list.Add( this.ParseFieldDecl( scope ) );
			}
			var rbrace = this.Expect( GoTokenID.RBRACE );

			var fields = new GoFieldList { Opening = lbrace, List = list, Closing = rbrace };
			return new GoStructType { Struct = pos, Fields = fields };
		}

		private GoField ParseFieldDecl( GoScope scope )
		{
			var doc = m_leadComment;

			List<GoExpr> list = null;
			GoExpr type = null;
			this.ParseVarList( out list, out type, false );

			GoBasicLit tag = null;
			if( m_tok == GoTokenID.STRING )
			{
				tag = new GoBasicLit { ValuePos = m_pos, Kind = m_tok, Value = m_lit };
				this.Next();
			}

			var idents = new List<GoIdent>();
			if( type != null )
			{
				idents = this.MakeIdentList( list );
			}
			else
			{
				type = list[0];
				if( list.Count > 1 || IsTypeName( Deref( type ) ) == false )
				{
					var pos = type.Pos;
					this.ErrorExpected( pos, "anonymous field" );
					type = new GoBadExpr { From = pos, To = this.SafePos( list.Last().End ) };
				}
			}

			this.ExpectSemi();

			var field = new GoField { Doc = doc, Names = idents, Type = type, Tag = tag, Comment = m_lineComment };
			this.Declare( field, null, scope, GoObjectKind.Var, idents.ToArray() );
			this.Resolve( type );

			return field;
		}

		private static GoExpr Deref( GoExpr x )
		{
			if( x is GoStarExpr )
			{
				var p = x as GoStarExpr;
				x = p.X;
			}
			return x;
		}

		private List<GoIdent> MakeIdentList( List<GoExpr> list )
		{
			var idents = new List<GoIdent>();
			foreach( var x in list )
			{
				var ident = x as GoIdent;
				if( x is GoIdent == false )
				{
					if( x is GoBadExpr == false )
					{
						this.ErrorExpected( x.Pos, "identifier" );
					}
					ident = new GoIdent { NamePos = x.Pos, Name = "_" };
				}
				idents.Add( ident );
			}
			return idents;
		}

		private void ParseVarList( out List<GoExpr> list, out GoExpr type, bool isParam )
		{
			list = new List<GoExpr>();
			type = this.ParseVarType( isParam );
			while( type != null )
			{
				list.Add( type );
				if( m_tok != GoTokenID.COMMA )
				{
					break;
				}
				this.Next();
				type = this.TryVarType( isParam );
			}

			type = this.TryVarType( isParam );
		}

		private GoExpr TryVarType( bool isParam )
		{
			if( isParam && m_tok == GoTokenID.ELLIPSIS )
			{
				var pos = m_pos;
				this.Next();
				var type = this.TryIdentOrType();
				if( type != null )
				{
					this.Resolve( type );
				}
				else
				{
					this.Error( pos, "'...' parameter is missing type" );
					type = new GoBadExpr { From = pos, To = m_pos };
				}
				return new GoEllipsis { Ellipsis = pos, Elt = type };
			}
			return this.TryIdentOrType();
		}

		private GoExpr ParseVarType( bool isParam )
		{
			var type = this.TryVarType( isParam );
			if( type == null )
			{
				var pos = m_pos;
				this.ErrorExpected( pos, "type" );
				this.Next();
				type = new GoBadExpr { From = pos, To = m_pos };
			}
			return type;
		}

		private GoExpr ParseArrayType()
		{
			var lbrack = this.Expect( GoTokenID.LBRACK );
			GoExpr len = null;
			if( m_tok == GoTokenID.ELLIPSIS )
			{
				len = new GoEllipsis { Ellipsis = m_pos };
				this.Next();
			}
			else if( m_tok != GoTokenID.RBRACK )
			{
				len = this.ParseRhs();
			}
			this.Expect( GoTokenID.RBRACK );
			var elt = this.ParseType();

			return new GoArrayType { Lbrack = lbrack, Len = len, Elt = elt };
		}

		private GoExpr ParseTypeName()
		{
			var ident = this.ParseIdent();

			if( m_tok == GoTokenID.PERIOD )
			{
				this.Next();
				this.Resolve( ident );
				var sel = this.ParseIdent();
				return new GoSelectorExpr { X = ident, Sel = sel };
			}

			return ident;
		}

		private GoExpr ParseSelector( GoExpr x )
		{
			var sel = this.ParseIdent();

			return new GoSelectorExpr { X = x, Sel = sel };
		}

		private GoExpr ParseOperand( bool lhs )
		{
			switch( m_tok )
			{
				case GoTokenID.IDENT:
					{
						var x = this.ParseIdent();
						if( lhs == false )
						{
							this.Resolve( x );
						}
						return x;
					}
				case GoTokenID.INT:
				case GoTokenID.FLOAT:
				case GoTokenID.IMAG:
				case GoTokenID.CHAR:
				case GoTokenID.STRING:
					{
						var x = new GoBasicLit { ValuePos = m_pos, Kind = m_tok, Value = m_lit };
						this.Next();
						return x;
					}
				case GoTokenID.LPAREN:
					{
						var lparen = m_pos;
						this.Next();
						m_exprLev++;
						var x = this.ParseRhsOrType();
						m_exprLev--;
						var rparen = this.Expect( GoTokenID.RPAREN );
						return new GoParenExpr { Lparen = lparen, X = x, Rparen = rparen };
					}
				case GoTokenID.FUNC:
					return this.ParseFuncTypeOrLit();
			}

			var type = this.TryIdentOrType();
			if( type != null )
			{
				var isIdent = type is GoIdent;
				this.Assert( isIdent == false, "Type cannot be identifier" );
				return type;
			}

			var pos = m_pos;
			this.ErrorExpected( pos, "operand" );
			SyncStmt( this );
			return new GoBadExpr { From = pos, To = m_pos };
		}

		private GoExpr ParseFuncTypeOrLit()
		{
			GoScope scope = null;
			var type = this.ParseFuncType( out scope );
			if( m_tok != GoTokenID.LBRACE )
			{
				return type;
			}

			m_exprLev++;
			var body = this.ParseBody( scope );
			m_exprLev--;

			return new GoFuncLit { Type = type, Body = body };
		}

		private GoExpr CheckExprOrType( GoExpr x )
		{
			var unparened = Unparen( x );
			if( unparened is GoParenExpr )
			{
				throw new Exception( "Unreachable" );
			}
			else if( unparened is GoUnaryExpr )
			{
			}
			else if( unparened is GoArrayType )
			{
				var t = unparened as GoArrayType;
				if( t.Len is GoEllipsis )
				{
					var len = t.Len as GoEllipsis;
					this.Error( len.Pos, "Expected array length, found '...'" );
					x = new GoBadExpr { From = x.Pos, To = this.SafePos( x.End ) };
				}
			}

			return x;
		}

		private List<GoExpr> ParseLhsList()
		{
			var old = m_inRhs;
			m_inRhs = false;
			var list = this.ParseExprList( true );
			switch( m_tok )
			{
				case GoTokenID.DEFINE:
					break;
				case GoTokenID.COLON:
					break;
				default:
					foreach( var x in list )
					{
						this.Resolve( x );
					}
					break;
			}
			m_inRhs = old;
			return list;
		}

		private List<GoExpr> ParseExprList( bool lhs )
		{
			var list = new List<GoExpr>();

			list.Add( this.CheckExpr( this.ParseExpr( lhs ) ) );
			while( m_tok == GoTokenID.COMMA )
			{
				this.Next();
				list.Add( this.CheckExpr( this.ParseExpr( lhs ) ) );
			}

			return list;
		}

		private GoStmt ParseSwitchStmt()
		{
			bool shouldCloseInnerScope = false;

			var pos = this.Expect( GoTokenID.SWITCH );
			this.OpenScope();

			GoStmt s1 = null, s2 = null;
			if( m_tok != GoTokenID.LBRACE )
			{
				var prevLev = m_exprLev;
				m_exprLev = -1;
				if( m_tok != GoTokenID.SEMICOLON )
				{
					this.ParseSimpleStmt( out s2, SimpleStmtParseMode.Basic );
				}
				if( m_tok == GoTokenID.SEMICOLON )
				{
					this.Next();
					s1 = s2;
					s2 = null;
					if( m_tok != GoTokenID.LBRACE )
					{
						this.OpenScope();
						shouldCloseInnerScope = true;
						this.ParseSimpleStmt( out s2, SimpleStmtParseMode.Basic );
					}
				}
				m_exprLev = prevLev;
			}

			var typeSwitch = IsTypeSwitchGuard( s2 );
			var lbrace = this.Expect( GoTokenID.LBRACE );
			var list = new List<GoStmt>();
			while( m_tok == GoTokenID.CASE || m_tok == GoTokenID.DEFAULT )
			{
				list.Add( this.ParseCaseClause( typeSwitch ) );
			}
			var rbrace = this.Expect( GoTokenID.RBRACE );
			this.ExpectSemi();
			var body = new GoBlockStmt { Lbrace = lbrace, List = list, Rbrace = rbrace };

			if( typeSwitch )
			{
				if( shouldCloseInnerScope )
				{
					this.CloseScope();
				}
				this.CloseScope();
				return new GoTypeSwitchStmt { Switch = pos, Init = s1, Assign = s2, Body = body };
			}

			if( shouldCloseInnerScope )
			{
				this.CloseScope();
			}
			this.CloseScope();
			return new GoSwitchStmt { Switch = pos, Init = s1, Tag = this.MakeExpr( s2, "switch expression" ), Body = body };
		}

		private GoStmt ParseCaseClause( bool typeSwitch )
		{
			var pos = m_pos;
			var list = new List<GoExpr>();
			if( m_tok == GoTokenID.CASE )
			{
				this.Next();
				if( typeSwitch )
				{
					list = this.ParseTypeList();
				}
				else
				{
					list = this.ParseRhsList();
				}
			}
			else
			{
				this.Expect( GoTokenID.DEFAULT );
			}

			var colon = this.Expect( GoTokenID.COLON );
			this.OpenScope();
			var body = this.ParseStmtList();
			this.CloseScope();

			return new GoCaseClause { Case = pos, List = list, Colon = colon, Body = body };
		}

		private List<GoExpr> ParseRhsList()
		{
			var old = m_inRhs;
			m_inRhs = true;
			var list = this.ParseExprList( false );
			m_inRhs = old;
			return list;
		}

		private List<GoExpr> ParseTypeList()
		{
			var list = new List<GoExpr>();

			list.Add( this.ParseType() );
			while( m_tok == GoTokenID.COMMA )
			{
				this.Next();
				list.Add( this.ParseType() );
			}

			return list;
		}

		private static bool IsTypeSwitchGuard( GoStmt s )
		{
			if( s is GoExprStmt )
			{
				var t = s as GoExprStmt;
				return IsTypeSwitchAssert( t.X );
			}
			else if( s is GoAssignStmt )
			{
				var t = s as GoAssignStmt;
				return t.Lhs.Count == 1 && t.Tok == GoTokenID.DEFINE && t.Rhs.Count == 1 && IsTypeSwitchAssert( t.Rhs[0] );
			}
			return false;
		}

		private static bool IsTypeSwitchAssert( GoExpr x )
		{
			bool ok = x is GoTypeAssertExpr;
			var a = x as GoTypeAssertExpr;
			return ok && a.Type == null;
		}

		private GoStmt ParseIfStmt()
		{
			var pos = this.Expect( GoTokenID.IF );
			this.OpenScope();

			GoStmt s = null;
			GoExpr x = null;

			var prevLev = m_exprLev;
			m_exprLev = -1;
			if( m_tok == GoTokenID.SEMICOLON )
			{
				this.Next();
				x = this.ParseRhs();
			}
			else
			{
				this.ParseSimpleStmt( out s, SimpleStmtParseMode.Basic );
				if( m_tok == GoTokenID.SEMICOLON )
				{
					this.Next();
					x = this.ParseRhs();
				}
				else
				{
					x = this.MakeExpr( s, "boolean expression" );
					s = null;
				}
			}
			m_exprLev = prevLev;

			var body = this.ParseBlockStmt();
			GoStmt else_ = null;
			if( m_tok == GoTokenID.ELSE )
			{
				this.Next();
				else_ = this.ParseStmt();
			}
			else
			{
				this.ExpectSemi();
			}

			this.CloseScope();
			return new GoIfStmt { If = pos, Init = s, Cond = x, Body = body, Else = else_ };
		}

		private GoBlockStmt ParseBlockStmt()
		{
			var lbrace = this.Expect( GoTokenID.LBRACE );
			this.OpenScope();
			var list = this.ParseStmtList();
			this.CloseScope();
			var rbrace = this.Expect( GoTokenID.RBRACE );

			return new GoBlockStmt { Lbrace = lbrace, List = list, Rbrace = rbrace };
		}

		private GoStmt ParseBranchStmt( GoTokenID tok )
		{
			var pos = this.Expect( tok );
			GoIdent label = null;
			if( tok != GoTokenID.FALLTHROUGH && m_tok == GoTokenID.IDENT )
			{
				label = this.ParseIdent();
				m_targetStack.Last().Add( label );
			}
			this.ExpectSemi();

			return new GoBranchStmt { TokPos = pos, Tok = tok, Label = label };
		}

		private GoStmt ParseReturnStmt()
		{
			var pos = m_pos;
			this.Expect( GoTokenID.RETURN );
			var x = new List<GoExpr>();
			if( m_tok != GoTokenID.SEMICOLON && m_tok != GoTokenID.RBRACE )
			{
				x = this.ParseRhsList();
			}
			this.ExpectSemi();

			return new GoReturnStmt { Return = pos, Results = x };
		}

		private GoStmt ParseDeferStmt()
		{
			var pos = this.Expect( GoTokenID.DEFER );
			var call = this.ParseCallExpr( "defer" );
			this.ExpectSemi();
			if( call == null )
			{
				return new GoBadStmt { From = pos, To = pos + 5 };
			}

			return new GoDeferStmt { Defer = pos, Call = call };
		}

		private GoCallExpr ParseCallExpr( string callType )
		{
			var x = this.ParseRhsOrType();
			if( x is GoCallExpr )
			{
				return x as GoCallExpr;
			}
			if( x is GoBadExpr == false )
			{
				this.Error( this.SafePos( x.End ), "Function must be invoked in " + callType + " statement" );
			}
			return null;
		}

		private GoStmt ParseGoStmt()
		{
			var pos = this.Expect( GoTokenID.GO );
			var call = this.ParseCallExpr( "go" );
			this.ExpectSemi();
			if( call == null )
			{
				return new GoBadStmt { From = pos, To = pos + 2 };
			}

			return new GoGoStmt { Go = pos, Call = call };
		}

		private bool ParseSimpleStmt( out GoStmt s, SimpleStmtParseMode mode )
		{
			var x = this.ParseLhsList();

			switch( m_tok )
			{
				case GoTokenID.DEFINE:
				case GoTokenID.ASSIGN:
				case GoTokenID.ADD_ASSIGN:
				case GoTokenID.SUB_ASSIGN:
				case GoTokenID.MUL_ASSIGN:
				case GoTokenID.QUO_ASSIGN:
				case GoTokenID.REM_ASSIGN:
				case GoTokenID.AND_ASSIGN:
				case GoTokenID.OR_ASSIGN:
				case GoTokenID.XOR_ASSIGN:
				case GoTokenID.SHL_ASSIGN:
				case GoTokenID.SHR_ASSIGN:
				case GoTokenID.AND_NOT_ASSIGN:
					{
						var pos = m_pos;
						var tok = m_tok;
						this.Next();
						var y = new List<GoExpr>();
						var isRange = false;
						if( mode == SimpleStmtParseMode.RangeOk && m_tok == GoTokenID.RANGE && ( tok == GoTokenID.DEFINE || tok == GoTokenID.ASSIGN ) )
						{
							var p = m_pos;
							this.Next();
							y.Add( new GoUnaryExpr { OpPos = p, Op = GoTokenID.RANGE, X = this.ParseRhs() } );
							isRange = true;
						}
						else
						{
							y = this.ParseRhsList();
						}
						var assign = new GoAssignStmt { Lhs = x, TokPos = pos, Tok = tok, Rhs = y };
						if( tok == GoTokenID.DEFINE )
						{
							this.ShortVarDecl( assign, x );
						}
						s = assign;
						return isRange;
					}
			}

			if( x.Count > 1 )
			{
				this.ErrorExpected( x[0].Pos, "1 expression" );
			}

			switch( m_tok )
			{
				case GoTokenID.COLON:
					{
						var colon = m_pos;
						this.Next();
						if( mode == SimpleStmtParseMode.LabelOk && x[0] is GoIdent )
						{
							var label = x[0] as GoIdent;
							var stmt = new GoLabeledStmt { Label = label, Colon = colon, Stmt = this.ParseStmt() };
							this.Declare( stmt, null, m_labelScope, GoObjectKind.Lbl, label );
							s = stmt;
							return false;
						}
						this.Error( colon, "Illegal label declaration" );
						s = new GoBadStmt { From = x[0].Pos, To = colon + 1 };
						return false;
					}
				case GoTokenID.ARROW:
					{
						var arrow = m_pos;
						this.Next();
						var y = this.ParseRhs();
						s = new GoSendStmt { Chan = x[0], Arrow = arrow, Value = y };
						return false;
					}
				case GoTokenID.INC:
				case GoTokenID.DEC:
					{
						s = new GoIncDecStmt { X = x[0], TokPos = m_pos, Tok = m_tok };
						this.Next();
						return false;
					}
			}

			s = new GoExprStmt { X = x[0] };
			return false;
		}

		private void OpenLabelScope()
		{
			m_labelScope = new GoScope( m_labelScope );
			m_targetStack.Add( new List<GoIdent>() );
		}

		private void ParseSignature( out GoFieldList parameters, out GoFieldList results, GoScope scope )
		{
			parameters = this.ParseParameters( scope, true );
			results = this.ParseResult( scope );
		}

		private GoFieldList ParseResult( GoScope scope )
		{
			if( m_tok == GoTokenID.LPAREN )
			{
				return this.ParseParameters( scope, false );
			}

			var type = this.TryType();
			if( type != null )
			{
				var list = new List<GoField>();
				list.Add( new GoField { Type = type } );
				return new GoFieldList { List = list };
			}

			return null;
		}

		private GoFieldList ParseParameters( GoScope scope, bool ellipsisOk )
		{
			var parameters = new List<GoField>();
			var lparen = this.Expect( GoTokenID.LPAREN );
			if( m_tok != GoTokenID.RPAREN )
			{
				parameters = this.ParseParameterList( scope, ellipsisOk );
			}
			var rparen = this.Expect( GoTokenID.RPAREN );

			return new GoFieldList { Opening = lparen, List = parameters, Closing = rparen };
		}

		private List<GoField> ParseParameterList( GoScope scope, bool ellipsisOk )
		{
			var parameters = new List<GoField>();

			List<GoExpr> list = null;
			GoExpr type = null;
			this.ParseVarList( out list, out type, ellipsisOk );

			if( type != null )
			{
				var idents = this.MakeIdentList( list );
				var field = new GoField { Names = idents, Type = type };
				parameters.Add( field );

				this.Declare( field, null, scope, GoObjectKind.Var, idents.ToArray() );
				this.Resolve( type );
				if( m_tok == GoTokenID.COMMA )
				{
					this.Next();
				}
				while( m_tok != GoTokenID.RPAREN && m_tok != GoTokenID.EOF )
				{
					var ids = this.ParseIdentList();
					var t = this.ParseVarType( ellipsisOk );
					var f = new GoField { Names = ids, Type = t };
					parameters.Add( f );

					this.Declare( f, null, scope, GoObjectKind.Var, ids.ToArray() );
					this.Resolve( t );
					if( this.AtComma( "parameter list" ) == false )
					{
						break;
					}
					this.Next();
				}
			}
			else
			{
				foreach( var t in list )
				{
					this.Resolve( t );
					parameters.Add( new GoField { Type = t } );
				}
			}

			return parameters;
		}

		private List<GoIdent> ParseIdentList()
		{
			var list = new List<GoIdent>();

			list.Add( this.ParseIdent() );
			while( m_tok == GoTokenID.COMMA )
			{
				this.Next();
				list.Add( this.ParseIdent() );
			}

			return list;
		}

		private GoFieldList ParseReceiver( GoScope scope )
		{
			var par = this.ParseParameters( scope, false );

			if( par.NumFields != 1 )
			{
				this.ErrorExpected( par.Opening, "exactly one receiver" );
				par.List.Clear();
				par.List.Add( new GoField { Type = new GoBadExpr { From = par.Opening, To = par.Closing + 1 } } );
				return par;
			}

			var recv = par.List[0];
			var b = Deref( recv.Type );
			if( b is GoIdent == false )
			{
				if( b is GoBadExpr == false )
				{
					this.ErrorExpected( b.Pos, "(unqualified) identifier" );
				}
				par.List.Clear();
				par.List.Add( new GoField { Type = new GoBadExpr { From = recv.Pos, To = this.SafePos( recv.End ) } } );
			}

			return par;
		}

		private GoSpec ParseTypeSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
		{
			var ident = this.ParseIdent();

			var spec = new GoTypeSpec { Doc = doc, Name = ident };
			this.Declare( spec, null, m_topScope, GoObjectKind.Typ, ident );

			spec.Type = this.ParseType();
			this.ExpectSemi();
			spec.Comment = m_lineComment;

			return spec;
		}

		private GoSpec ParseValueSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
		{
			var idents = this.ParseIdentList();
			var type = this.TryType();
			var values = new List<GoExpr>();

			if( m_tok == GoTokenID.ASSIGN )
			{
				this.Next();
				values = this.ParseRhsList();
			}
			this.ExpectSemi();

			var spec = new GoValueSpec { Doc = doc, Names = idents, Type = type, Values = values, Comment = m_lineComment };
			var kind = GoObjectKind.Con;
			if( keyword == GoTokenID.VAR )
			{
				kind = GoObjectKind.Var;
			}
			this.Declare( spec, iota, m_topScope, kind, idents.ToArray() );

			return spec;
		}

		private GoSpec ParseImportSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
		{
			GoIdent ident = null;
			switch( m_tok )
			{
				case GoTokenID.PERIOD:
					ident = new GoIdent { NamePos = m_pos, Name = "." };
					this.Next();
					break;
				case GoTokenID.IDENT:
					ident = this.ParseIdent();
					break;
			}

			var pos = m_pos;
			var path = "";
			if( m_tok == GoTokenID.STRING )
			{
				path = m_lit;
				if( IsValidImport( path ) == false )
				{
					this.Error( pos, "Invalid import path: " + path );
				}
				this.Next();
			}
			else
			{
				this.Expect( GoTokenID.STRING );
			}
			this.ExpectSemi();

			var spec = new GoImportSpec { Doc = doc, Name = ident, Path = new GoBasicLit { ValuePos = pos, Kind = GoTokenID.STRING, Value = path }, Comment = m_lineComment };
			m_imports.Add( spec );

			return spec;
		}

		private static bool IsValidImport( string lit )
		{
			var illegalChars = @"!#$%&'()*,:;<=>?[\]^{|}" + "\"`\uFFFD";
			var s = Unquote( lit );
			foreach( var r in s )
			{
				if( char.IsSymbol( r ) == false || char.IsWhiteSpace( r ) || illegalChars.Contains( r ) )
				{
					return false;
				}
			}
			return s != "";
		}

		private static string Unquote( string str )
		{
			return str.Trim( '"', '\'', '`' );
		}

		private GoGenDecl ParseGenDecl( GoTokenID keyword, ParseSpecFunction f )
		{
			var doc = m_leadComment;
			var pos = this.Expect( keyword );
			int lparen = 0, rparen = 0;
			var list = new List<GoSpec>();
			if( m_tok == GoTokenID.LPAREN )
			{
				lparen = m_pos;
				this.Next();
				for( var iota = 0; m_tok != GoTokenID.RPAREN && m_tok != GoTokenID.EOF; iota++ )
				{
					list.Add( f( m_leadComment, keyword, iota ) );
				}
				rparen = this.Expect( GoTokenID.RPAREN );
				this.ExpectSemi();
			}
			else
			{
				list.Add( f( null, keyword, 0 ) );
			}

			return new GoGenDecl { Doc = doc, TokPos = pos, Tok = keyword, Lparen = lparen, Specs = list, Rparen = rparen };
		}

		private void ExpectSemi()
		{
			if( m_tok != GoTokenID.RPAREN && m_tok != GoTokenID.RBRACE )
			{
				if( m_tok == GoTokenID.SEMICOLON )
				{
					this.Next();
				}
				else
				{
					this.ErrorExpected( m_pos, "';'" );
					SyncStmt( this );
				}
			}
		}

		private GoIdent ParseIdent()
		{
			var pos = m_pos;
			var name = "_";
			if( m_tok == GoTokenID.IDENT )
			{
				name = m_lit;
				this.Next();
			}
			else
			{
				this.Expect( GoTokenID.IDENT );
			}
			return new GoIdent { NamePos = pos, Name = name };
		}

		private int Expect( GoTokenID tok )
		{
			var pos = m_pos;
			if( m_tok != tok )
			{
				this.ErrorExpected( pos, "'" + GoToken.ToString( tok ) + "'" );
			}
			this.Next();
			return pos;
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
