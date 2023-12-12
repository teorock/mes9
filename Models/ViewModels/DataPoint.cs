using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class DataPoint
    {
		public DataPoint(string label, double y, string xValue)
		{
			this.Label = label;
			this.Y = y;
			this.xValue = xValue;
		}
 
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "label")]
		public string Label = "";
 
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public Nullable<double> Y = null;

		[DataMember(Name = "xValue")]
		public string xValue = null;  		   
    }
}