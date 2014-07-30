using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoScope
	{
		public GoScope Outer { get; set; }

		public Dictionary<string, GoObject> Objects { get; set; }

		public GoScope( GoScope outer )
		{
			this.Outer = outer;
			this.Objects = new Dictionary<string, GoObject>( 4 );
		}

		public GoObject Lookup( string name )
		{
			GoObject result = null;
			if( this.Objects.TryGetValue( name, out result ) )
			{
				return result;
			}
			return null;
		}

		public GoObject Insert( GoObject obj )
		{
			var alt = this.Lookup( obj.Name );
			if( alt == null )
			{
				this.Objects.Add( obj.Name, obj );
			}
			return alt;
		}
	}
}
