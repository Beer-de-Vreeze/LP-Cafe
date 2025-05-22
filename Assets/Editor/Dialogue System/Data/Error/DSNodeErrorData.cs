using UnityEngine;
using System.Collections.Generic;

namespace LPCafe.Data.Error
{
    using Elements;

    public class DSNodeErrorData
    {
        public DSErrorData m_errorData { get; set;}
        public List<NodeBase> m_nodesErrorData { get; set;}

        public DSNodeErrorData()
        {
            m_errorData = new DSErrorData();
            m_nodesErrorData = new List<NodeBase>();
        }
    }
}