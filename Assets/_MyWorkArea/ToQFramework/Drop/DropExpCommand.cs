using QFramework;
using UnityEngine;


namespace QFramework.Car
{
    public class DropExpCommand : AbstractCommand
    {
        private Vector3 m_pos;

        public DropExpCommand(Vector3 pos)
        {
            m_pos = pos;
        }

        protected override void OnExecute()
        {
            GameObject.Instantiate(ResLoader.Allocate().LoadSync<GameObject>("Exp"), m_pos, Quaternion.identity);
        }


    }
}