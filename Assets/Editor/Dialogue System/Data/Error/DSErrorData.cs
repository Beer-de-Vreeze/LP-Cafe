using UnityEngine;


namespace LPCafe.Data.Error
{
    public class DSErrorData
    {
        public  Color m_color { get; set; }

        public DSErrorData()
        {
            GenerateRandomColor();
        }

        private void GenerateRandomColor()
        {
            m_color = new Color32
            (
                (byte) Random.Range(65, 256),
                (byte) Random.Range(50, 176),
                (byte) Random.Range(50, 176),
                255
            );
            
        }
    }
}