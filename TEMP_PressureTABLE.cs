using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HHCSService
{
    public class TEMP_PressureTABLE
    {
        public int bp_id_pointer { get; set; }
        public DateTime bp_time_new { get; set; }
        public DateTime bp_time_old { get; set; }
        public string bp_time_string_new { get; set; }
        public int bp_up_new { get; set; }
        public int bp_up_old { get; set; }
        public int bp_lo_new { get; set; }
        public int bp_lo_old { get; set; }
        public int bp_hr_new { get; set; }
        public int bp_hr_old { get; set; }
        public int bp_up_lvl_new { get; set; }
        public int bp_up_lvl_old { get; set; }
        public int bp_lo_lvl_new { get; set; }
        public int bp_lo_lvl_old { get; set; }
        public int bp_hr_lvl_new { get; set; }
        public int bp_hr_lvl_old { get; set; }
        public string mode { get; set; }
    }
}