using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoErrorList
	{
		private List<GoError> m_errors;

		public List<GoError> Errors
		{
			get { return m_errors; }
		}

		public int Length
		{
			get
			{
				return m_errors.Count;
			}
		}

		public GoErrorList()
		{
			m_errors = new List<GoError>();
		}

		public void Add( GoSourcePosition pos, string msg )
		{
			m_errors.Add( new GoError( pos, msg ) );
		}

		public void Reset()
		{
			m_errors.Clear();
		}

		public void Swap( int i, int j )
		{
			var swapper = m_errors[i];
			m_errors[i] = m_errors[j];
			m_errors[j] = swapper;
		}

		// TODO: Several methods left to implement.
	}
}
