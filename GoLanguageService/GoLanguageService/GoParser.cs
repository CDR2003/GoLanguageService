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

		private GoScanner m_scanner;

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

		public GoParser( string filename, string text )
		{

		}
	}
}
