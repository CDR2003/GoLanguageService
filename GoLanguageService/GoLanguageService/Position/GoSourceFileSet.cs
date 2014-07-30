using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoSourceFileSet
	{
		public int Base { get; private set; }

		private List<GoSourceFile> m_files;

		private GoSourceFile m_last;

		public GoSourceFileSet()
		{
			this.Base = 1;
			m_files = new List<GoSourceFile>();
			m_last = null;
		}

		public GoSourceFile AddFile( string filename, int b, int size )
		{
			if( b < 0 )
			{
				b = this.Base;
			}
			if( b < this.Base || size < 0 )
			{
				throw new ArgumentException( "Illegal base or size" );
			}

			var f = new GoSourceFile( this, filename, b, size );
			b += size + 1;
			if( b < 0 )
			{
				throw new Exception( "token.Pos offset overflow (> 2G of source code in file set)" );
			}

			this.Base = b;
			m_files.Add( f );
			m_last = f;
			return f;
		}

		public void Iterate( Func<GoSourceFile, bool> f )
		{
			foreach( var file in m_files )
			{
				if( f( file ) == false )
				{
					break;
				}
			}
		}

		public GoSourceFile File( int p )
		{
			if( p != 0 )
			{
				return this.DoFile( p );
			}
			return null;
		}

		public GoSourcePosition Position( int p )
		{
			if( p != 0 )
			{
				var f = this.DoFile( p );
				if( f != null )
				{
					return f.Position( p );
				}
			}
			return new GoSourcePosition();
		}

		private static int SearchFiles( List<GoSourceFile> a, int x )
		{
			return a.FindIndex( ( file ) => file.Base > x );
		}

		private GoSourceFile DoFile( int p )
		{
			if( m_last != null && m_last.Base <= p && p <= m_last.Base + m_last.Size )
			{
				return m_last;
			}

			var i = SearchFiles( m_files, p );
			if( i >= 0 )
			{
				var f = m_files[i];
				if( p <= f.Base + f.Size )
				{
					m_last = f;
					return f;
				}
			}
			return null;
		}
	}
}
