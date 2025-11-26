using NUnit.Framework;
using UnityEngine.TestTools;

// Ensures noisy GUID conflict logs emitted by package/asset load during test runner startup
// are marked as expected so they don't cause unhandled log failures.
[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Historically some environments logged GUID conflict errors during test startup.
        // That issue has been fixed; no global log expectations are required anymore.
    }
}
