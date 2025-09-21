using Fusion;
using UnityEngine;

namespace Test
{
    public class TestPlayer : NetworkBehaviour
    {
        public override void Spawned()
        {
            base.Spawned();
            ShowLog();
        }

        public void ShowLog()
        {
            Debug.Log($"Index {Runner.LocalPlayer.PlayerId}\nHas state auth {HasStateAuthority}\nHas Input Auth {HasInputAuthority}");
        }
    }
}