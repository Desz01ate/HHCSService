using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHCSService
{
    public class TEMP_DiabetesTABLE
    {

        public int fbs_id_pointer { get; set; }
        public DateTime fbs_time_new { get; set; }
        public string fbs_time_string_new { get; set; }
        public DateTime fbs_time_old { get; set; }
        public int fbs_fbs_new { get; set; }
        public int fbs_fbs_old { get; set; }
        public int fbs_fbs_lvl_new { get; set; }
        public int fbs_fbs_lvl_old { get; set; }
        public double fbs_fbs_sum_new { get; set; }
        public double fbs_fbs_sum_old { get; set; }
        public string mode { get; set; }


        public TEMP_DiabetesTABLE() { }

    }
}