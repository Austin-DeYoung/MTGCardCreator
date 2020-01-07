using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGCardCreator
{
    class PricePrediction
    {
        [ColumnName("Score")]
        public float Price;
    }
}
