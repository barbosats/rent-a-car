using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fnPaymanet
{
    internal class PaymanetModel
    {
        public Guid id { get { return Guid.NewGuid(); } set; }
        public Guid IdPaymant { get { return Guid.NewGuid(); } set; }
        public string nome { get; set; }
        public string email { get; set; }
        public string modelo { get; set; }
        public int ano { get; set; }
        public string tempoAluguel { get; set; }
        public DateTime data { get; set; }
        public string status { get; set; }
        public DateTime? dataAprovacao { get; set; }
    }

}
