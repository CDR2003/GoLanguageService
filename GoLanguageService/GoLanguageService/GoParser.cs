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
				unparened is GoCompositeLit ||
				unparened is GoParenExpr )
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

		private object Unparen( GoExpr x )
		{
			throw new NotImplementedException();
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
			if( x is GoBadExpr )
			{
			}
		}

		private static bool IsLiteralType( GoExpr x )
		{
			throw new NotImplementedException();
		}

		private GoExpr ParseCallOrConversion( GoExpr goExpr )
		{
			throw new NotImplementedException();
		}

		private GoExpr ParseIndexOrSlice( GoExpr goExpr )
		{
			throw new NotImplementedException();
		}

		private GoExpr ParseTypeAssertion( GoExpr goExpr )
		{
			throw new NotImplementedException();
		}

		private GoExpr ParseSelector( GoExpr goExpr )
		{
			throw new NotImplementedException();
		}

		private GoExpr ParseOperand( bool lhs )
		{
			throw new NotImplementedException();
		}

		private GoExpr CheckExprOrType( GoExpr x )
		{
			throw new NotImplementedException();
		}

		private List<GoExpr> ParseLhsList()
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseSwitchStmt()
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseIfStmt()
		{
			throw new NotImplementedException();
		}

		private GoBlockStmt ParseBlockStmt()
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseBranchStmt( GoTokenID tok )
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseReturnStmt()
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseDeferStmt()
		{
			throw new NotImplementedException();
		}

		private GoStmt ParseGoStmt()
		{
			throw new NotImplementedException();
		}

		private bool ParseSimpleStmt( out GoStmt s, SimpleStmtParseMode simpleStmtParseMode )
		{
			throw new NotImplementedException();
		}

		private void OpenLabelScope()
		{
			throw new NotImplementedException();
		}

		private void ParseSignature( out GoFieldList parameters, out GoFieldList results, GoScope scope )
		{
			throw new NotImplementedException();
		}

		private GoFieldList ParseReceiver( GoScope scope )
		{
			throw new NotImplementedException();
		}

		private GoSpec ParseTypeSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
		{
			throw new NotImplementedException();
		}

		private GoSpec ParseValueSpec( GoCommentGroup doc, GoTokenID keyword, int iota )
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
