using NUnit.Framework;
using FbxSdk;

namespace UnitTests
{

    public class FbxIOSettingsTest : Base<FbxIOSettings>
    {
        [Test]
        public void TestFVirtual ()
        {
            // Test the swig -fvirtual flag works properly: we can call virtual
            // functions defined on the base class without the function also
            // being defined in the subclass.

            FbxManager manager = FbxManager.Create ();
            FbxIOSettings ioSettings = FbxIOSettings.Create (manager, "");

            // GetSelected is a virtual method inherited from FbxObject
            Assert.IsFalse (ioSettings.GetSelected ());
            ioSettings.SetSelected (true);
            Assert.IsTrue (ioSettings.GetSelected ());

            ioSettings.Destroy ();
            manager.Destroy ();
        }
    }
}