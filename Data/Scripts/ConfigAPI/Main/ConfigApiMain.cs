using VRage.Game.Components;

namespace mz.NewTemplateMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]  
    public class ConfigApiMain : MySessionComponentBase
    {
        public override void LoadData()
        {
            base.LoadData();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
        }
    }
}