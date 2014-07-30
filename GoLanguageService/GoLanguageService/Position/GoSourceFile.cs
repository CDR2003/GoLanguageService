using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoSourceFile
	{
		public string Name { get; private set; }

		public int Base { get; private set; }

		public int Size { get; private set; }

		public int LineCount
		{
			get
			{
				return m_lines.Count;
			}
		}

		private GoSourceFileSet m_set;

		private List<int> m_lines;

		private List<GoSourceLineInfo> m_infos;

		public GoSourceFile( GoSourceFileSet set, string filename, int b, int size )
		{
			m_set = set;
			this.Name = filename;
			this.Base = b;
			this.Size = size;

			m_lines = new List<int>();
			m_lines.Add( 0 );

			m_infos = new List<GoSourceLineInfo>();
		}

		public void AddLine( int offset )
		{
			if( ( m_lines.Count == 0 || m_lines.Last() < offset ) && offset < this.Size )
			{
				m_lines.Add( offset );
			}
		}

		public void MergeLine( int line )
		{
			if( line <= 0 )
			{
				throw new ArgumentException( "Illegal line number (line numbering starts at 1)", "line" );
			}

			if( line >= m_lines.Count )
			{
				throw new ArgumentException( "Illegal line number", "line" );
			}

			m_lines.RemoveAt( line + 1 );
		}

		public bool SetLines( List<int> lines )
		{
			var size = this.Size;
			for( int i = 0; i < lines.Count; i++ )
			{
				var offset = lines[i];
				if( ( i > 0 && offset <= lines.Last() ) || size <= offset )
				{
					return false;
				}
			}

			m_lines = lines;
			return true;
		}

		public void SetLinesForContent( string content )
		{
			var lines = new List<int>();
			var line = 0;
			for( int offset = 0; offset < content.Length; offset++ )
			{
				var b = content[offset];
				if( line >= 0 )
				{
					lines.Add( line );
				}
				line = -1;
				if( b == '\n' )
				{
					line = offset + 1;
				}
			}

			m_lines = lines;
		}

		public void AddLineInfo( int offset, string filename, int line )
		{
			if( m_infos.Count == 0 || ( m_infos.Last().Offset < offset && offset < this.Size ) )
			{
				m_infos.Add( new GoSourceLineInfo( offset, filename, line ) );
			}
		}

		public int Pos( int offset )
		{
			if( offset > this.Size )
			{
				throw new ArgumentException( "Illegal file offset", "offset" );
			}

			return this.Base + offset;
		}

		public int Offset( int p )
		{
			if( p < this.Base || p > this.Base + this.Size )
			{
				throw new ArgumentException( "Illegal Pos value", "p" );
			}
			return p - this.Base;
		}

		public int Line( int p )
		{
			return this.Position( p ).Line;
		}

		public GoSourcePosition Position( int p )
		{
			if( p != 0 )
			{
				if( p < this.Base || p > this.Base + this.Size )
				{
					throw new ArgumentException( "Illegal Pos value", "p" );
				}
				return this.DoPosition( p );
			}
			return new GoSourcePosition();
		}

		private static int SearchLineInfos( List<GoSourceLineInfo> a, int x )
		{
			return a.FindIndex( ( info ) => info.Offset > x );
		}

		private static int SearchInts( List<int> a, int x )
		{
			return a.FindIndex( ( i ) => i > x );
		}

		private GoSourceLineInfo Info( int offset )
		{
			var info = new GoSourceLineInfo();
			info.Filename = this.Name;

			var i = SearchInts( m_lines, offset );
			if( i >= 0 )
			{
				info.Line = i + 1;
				info.Offset = offset - m_lines[i] + 1;
			}
			if( m_infos.Count > 0 )
			{
				var j = SearchLineInfos( m_infos, offset );
				if( j >= 0 )
				{
					var alt = m_infos[j];
					info.Filename = alt.Filename;
					var k = SearchInts( m_lines, alt.Offset );
					if( k >= 0 )
					{
						info.Line += alt.Line - i - 1;
					}
				}
			}
			return info;
		}

		private GoSourcePosition DoPosition( int p )
		{
			var pos = new GoSourcePosition();
			var offset = p - this.Base;
			pos.Offset = offset;

			var info = this.Info( offset );
			pos.Filename = info.Filename;
			pos.Line = info.Line;
			pos.Column = info.Offset;

			return pos;
		}
	}
}
