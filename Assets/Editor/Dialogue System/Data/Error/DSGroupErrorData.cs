using System.Collections.Generic;

namespace DS.Data.Error
{
    using Elements;

    public class DSGroupErrorData
    {
        public DSErrorData m_errorData { get; set; }
        public List<DSGroup> m_errorDatagroups { get; set; }

        public DSGroupErrorData() 
        { 
            m_errorData = new DSErrorData();
            m_errorDatagroups = new List<DSGroup>();
        }
    }
}