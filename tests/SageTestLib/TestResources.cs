/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Core;
using Highpoint.Sage.Materials;
using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Highpoint.Sage.Resources
{


    public class ResourceTester : IDisposable
    {

        public ResourceTester()
        {
            Init();
        }


        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
                    GC.SuppressFinalize(this);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Allocates and deallocates resources and checks the behavior")]
        public void TestPersistentResourceBasics()
        {

            Model model = new Model("Resource Testing Model...");

            SelfManagingResource steamSystem = new SelfManagingResource(model, "SteamSystem", Guid.NewGuid(), 7000.0, false, false, true);

            ResourceRequest[] requests = new ResourceRequest[7];
            for (int i = 0; i < 7; i++)
            {
                requests[i] = new ResourceRequest(1000.0);
            }

            for (int i = 0; i < 7; i++)
            {
                if (steamSystem.Reserve(requests[i], false))
                {
                    double obtained = requests[i].QuantityObtained;
                    double remaining = requests[i].ResourceObtained.Available;
                    Debug.WriteLine("Successfully reserved " + obtained + " pounds of steam - " + remaining + " remains.");
                }
                else
                {
                    Debug.WriteLine("Failed to reserve steam for request[" + i + "]");
                }
            }

            Debug.WriteLine("Unreserving steam from 2 requests");
            steamSystem.Unreserve(requests[2]);
            double available = steamSystem.Available;
            Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

            steamSystem.Unreserve(requests[3]);
            available = steamSystem.Available;
            Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

            for (int i = 5; i < 7; i++)
            {
                if (steamSystem.Reserve(requests[i], false))
                {
                    double obtained = requests[i].QuantityObtained;
                    double remaining = requests[i].ResourceObtained.Available;
                    Debug.WriteLine("Successfully reserved " + obtained + " pounds of steam - " + remaining + " remains.");
                }
                else
                {
                    Debug.WriteLine("Failed to acquire steam for request[" + i + "]");
                }
            }


            Debug.WriteLine("Unreserving all steam requests - ");
            for (int i = 0; i < 7; i++)
                steamSystem.Unreserve(requests[i]);
            available = steamSystem.Available;
            Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

            // AEL, bug "Reserve a resource over an existing one" submitted.
            //			Debug.WriteLine("Trying to acquire all steam requests - ");
            //			for ( int i = 0; i < 7 ; i++ ) {
            //				if ( steamSystem.Acquire(requests[i],false) ){
            //					double obtained = requests[i].QuantityObtained;
            //					double remaining = requests[i].ResourceObtained.Available;
            //					Debug.WriteLine("Successfully acquired " + obtained + " pounds of steam - " + remaining + " remains.");
            //				} else {
            //					Debug.WriteLine("Failed to acquire steam for request["+i+"]");
            //				}
            //			}
            //
            //			Debug.WriteLine("Releasing 2 steam requests ");
            //			steamSystem.Release(requests[1]);
            //			steamSystem.Release(requests[4]);
            //
            //			for ( int i = 5; i < 7 ; i++ ) {
            //				if ( steamSystem.Acquire(requests[i],false) ){
            //					double obtained = requests[i].QuantityObtained;
            //					double remaining = requests[i].ResourceObtained.Available;
            //					Debug.WriteLine("Successfully acquired " + obtained + " pounds of steam - " + remaining + " remains.");
            //				} else {
            //					Debug.WriteLine("Failed to acquire steam for request["+i+"]");
            //				}
            //			}

            Debug.WriteLine("Releasing all steam requests - ");
            for (int i = 0; i < 7; i++)
                steamSystem.Release(requests[i]);

            //model.Validate();
            model.Start();

        }


        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks if resources can be augmented or depleted")]
        public void TestConsumableResourceBasics()
        {

            Model model = new Model("Resource Testing Model...");
            Debug.WriteLine("Test results of replenishable material inventories.");

            Hashtable materialInventory = new Hashtable();

            Debug.WriteLine("Setting up an inventory of 500 liters Water, capacity of 1000 liters.");
            MaterialType waterType = new MaterialType(model, "Water", Guid.NewGuid(), 1.0, 1.0, MaterialState.Liquid, 18.0);
            MaterialResourceItem waterItem = new MaterialResourceItem(model, waterType, 500, 20, 1000);

            Debug.WriteLine("Setting up an inventory of 100 liters SodiumChloride, capacity of 250 liters.");
            MaterialType NaClType = new MaterialType(model, "SodiumChloride", Guid.NewGuid(), 1.2, 1.8, MaterialState.Liquid);
            MaterialResourceItem NaClItem = new MaterialResourceItem(model, NaClType, 100, 20, 250);

            materialInventory.Add(waterType, waterItem);
            materialInventory.Add(NaClType, NaClItem);

            Enum augment = MaterialResourceRequest.Direction.Augment;
            Enum deplete = MaterialResourceRequest.Direction.Deplete;
            object[,] tests = new object[,]{    {waterType,150,augment,true}
                                               ,{waterType,250,deplete,true}
                                               ,{waterType,350,augment,true}
                                               ,{NaClType,200,deplete,false}
                                               ,{waterType,100,deplete,true}
                                               ,{NaClType,300,augment,false}
                                               ,{waterType,200,deplete,true}
                                               ,{NaClType,300,augment,false}
                                               ,{waterType,100,deplete,true}
                                               ,{NaClType,250,deplete,false}
                                               ,{NaClType,1000,augment,false}};
            MaterialResourceRequest mrr;
            Debug.WriteLine("");
            for (int i = 0; i < tests.GetLength(0); i++)
            {

                #region >>> Set up test parameters from array. <<<
                MaterialType mt = (MaterialType)tests[i, 0];
                double quantity = Convert.ToDouble(tests[i, 1]);
                MaterialResourceRequest.Direction direction = (MaterialResourceRequest.Direction)tests[i, 2];
                string itemName = (mt.Equals(waterType) ? "WaterItem" : "NaClItem");
                MaterialResourceItem item = (mt.Equals(waterType) ? waterItem : NaClItem);
                bool expected = (bool)tests[i, 3];
                #endregion

                string testDescription = "Test " + (i + 1) + ": Trying to " + (direction.Equals(augment) ? "augment" : "deplete") + " " + quantity + " liters of " + itemName + ".";
                Debug.WriteLine(testDescription);
                Debug.WriteLine("Before - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
                mrr = new MaterialResourceRequest(mt, quantity, direction);
                MaterialResourceItem mri = (MaterialResourceItem)materialInventory[mt];
                bool result = mri.Acquire(mrr, false);
                Debug.Write((result ? "Request honored." : "Request denied."));
                Assert.True(result == expected, "This test is a failure");
                Debug.WriteLine(((result == expected) ? " - this was expected." : " - THIS IS A TEST FAILURE!"));
                Assert.True(expected == result, testDescription);
                Debug.WriteLine("After - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
                Debug.WriteLine("");
            }

        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
        public void TestMaterialConduits()
        {

            Model model = new Model("Conduit Testing Model...");
            Debug.WriteLine("Test results of replenishable material inventories.");

            Hashtable materialInventory1 = new Hashtable();
            Hashtable materialInventory2 = new Hashtable();
            Hashtable materialInventory3 = new Hashtable();
            MaterialType WaterType = new MaterialType(model, "Water", Guid.NewGuid(), 1.0, 1.0, MaterialState.Liquid);
            MaterialType NaClType = new MaterialType(model, "SodiumChloride", Guid.NewGuid(), 1.2, 1.8, MaterialState.Solid);
            MaterialResourceItem WaterItem = new MaterialResourceItem(model, WaterType, 500, 20, 1000);
            MaterialResourceItem NaClItem = new MaterialResourceItem(model, NaClType, 100, 20, 250);

            Debug.WriteLine("Setting up an inventory of 500 liters Water, capacity of 1000 liters in materialInventory1.");
            materialInventory1.Add(WaterType, WaterItem);

            Debug.WriteLine("Setting up an inventory of 100 liters SodiumChloride, capacity of 250 liters in materialInventory1.");
            materialInventory1.Add(NaClType, NaClItem);

            Debug.WriteLine("Setting up an inventory of 750 liters Water, capacity of 1500 liters in materialInventory2.");
            materialInventory2.Add(WaterType, new MaterialResourceItem(model, WaterType, 750, 20, 1500));

            Debug.WriteLine("Setting up an inventory of 400 liters SodiumChloride, capacity of 800 liters in materialInventory3.");
            materialInventory3.Add(NaClType, new MaterialResourceItem(model, NaClType, 400, 20, 800));

            // AEL, not sure why thest lines don't matter in the test. I probably miss something.
            //			MaterialConduitManager mcm = new MaterialConduitManager(materialInventory1);
            //			mcm.AddConduit(materialInventory2,WaterType);
            //			mcm.AddConduit(materialInventory3,NaClType);

            Enum augment = MaterialResourceRequest.Direction.Augment;
            Enum deplete = MaterialResourceRequest.Direction.Deplete;
            object[,] tests = new object[,]{    {WaterType,150,augment,true}
                                               ,{WaterType,250,deplete,true}
                                               ,{WaterType,350,augment,true}
                                               ,{NaClType,200,deplete,false}
                                               ,{WaterType,100,deplete,true}
                                               ,{NaClType,300,augment,false}
                                               ,{WaterType,200,deplete,true}
                                               ,{NaClType,300,augment,false}
                                               ,{WaterType,100,deplete,true}
                                               ,{NaClType,250,deplete,false}
                                               ,{NaClType,1000,augment,false}};
            MaterialResourceRequest mrr;
            Debug.WriteLine("");
            for (int i = 0; i < tests.GetLength(0); i++)
            {

                #region >>> Set up test parameters from array. <<<
                MaterialType mt = (MaterialType)tests[i, 0];
                double quantity = Convert.ToDouble(tests[i, 1]);
                MaterialResourceRequest.Direction direction = (MaterialResourceRequest.Direction)tests[i, 2];
                string itemName = (mt.Equals(WaterType) ? "WaterItem" : "NaClItem");
                MaterialResourceItem item = (mt.Equals(WaterType) ? WaterItem : NaClItem);
                bool expected = (bool)tests[i, 3];
                #endregion

                string testDescription = "Test " + (i + 1) + ": Trying to " + (direction.Equals(augment) ? "augment" : "deplete") + " " + quantity + " liters of " + itemName + ".";
                Debug.WriteLine(testDescription);
                Debug.WriteLine("Before - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
                mrr = new MaterialResourceRequest(mt, quantity, direction);
                MaterialResourceItem mri = (MaterialResourceItem)materialInventory1[mt];
                bool result = mri.Acquire(mrr, false);
                Debug.Write((result ? "Request honored." : "Request denied."));
                Assert.True(result == expected, "This test is a failure");
                Debug.WriteLine(((result == expected) ? " - this was expected." : " - THIS IS A TEST FAILURE!"));
                Assert.True(result == expected, testDescription);
                Debug.WriteLine("After - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
                Debug.WriteLine("");
            }

        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
        public void TestEarmarking()
        {
            Model model = new Model("Test model");

            object genericAccessKey = new object();

            Resource rsc1 = new Resource(model, "Rsc 1", Guid.NewGuid(), 2.0, 2.0, true, true, true);
            Resource rsc2 = new Resource(model, "Rsc 2", Guid.NewGuid(), 2.0, 2.0, true, true, true);

            ResourceManager rscPool1 = new ResourceManager(model, "pool 1", Guid.NewGuid());
            rscPool1.Add(rsc1);
            rscPool1.Add(rsc2);

            // First, add an AccessManager to the resource pool. Then push a SingleKeyAccessRegulator
            // onto the AccessManager's stack - the SingleKeyAccessRegulator will permit only resource
            // requests that hold a reference to the 'genericAccessKey'.
            rscPool1.AccessRegulator = new SimpleAccessManager(true);
            ((SimpleAccessManager)rscPool1.AccessRegulator).PushAccessRegulator(new SingleKeyAccessRegulator(null, genericAccessKey), null);

            // Create and try to use a resource request that has the generic access key.
            ResourceRequest rr = new ResourceRequest(1.0);
            rr.Key = genericAccessKey;

            rscPool1.Acquire(rr, false); // This should succeed.
            Debug.WriteLine(rr.ResourceObtained == null ? "Resource not obtained." : "Resource obtained.");
            rr.Release();

            // Now change the key to 'just an object' - i.e. one that the AccessManager doesn't recognize.
            rr.Key = new object();
            rscPool1.Acquire(rr, false); // This should fail.
            Debug.WriteLine(rr.ResourceObtained == null ? "Resource not obtained" : "Resource obtained.");


        }


        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
        public void TestAdvancedEarmarking()
        {
            Model model = new Model("Test model");

            SelfManagingResource rsc = new SelfManagingResource(model, "Resource", Guid.NewGuid(), 2.0, 2.0, true, true, true);
            rsc.AccessRegulator = new SimpleAccessManager(true);

            object keyA = "Key A"; // Always grants to any 'A' holders.
            object keyA1 = "Key A1"; // Sometimes grants to 'A1' holders.
            object keyA2 = "Key A2"; // Sometimes grants to 'A2' holders.

            IAccessRegulator reg1 = new MultiKeyAccessRegulator(rsc, new ArrayList(new object[] { keyA, keyA1 }));
            IAccessRegulator reg2 = new MultiKeyAccessRegulator(rsc, new ArrayList(new object[] { keyA, keyA2 }));

            ResourceRequest rrTrack1 = new ResourceRequest(1.0);
            rrTrack1.Key = keyA1;
            ResourceRequest rrTrack2 = new ResourceRequest(1.0);
            rrTrack2.Key = keyA2;
            ResourceRequest rrKahuna = new ResourceRequest(1.0);
            rrKahuna.Key = keyA;

            /////////////////////////////////////////////////////////////////////////////
            // Test A: Track 1 acquires equipment, installs key. Track 2 tries and fails.
            ((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg1, null);
            TryAcquire(rsc, rrTrack1, true);
            TryAcquire(rsc, rrTrack2, false);

            /////////////////////////////////////////////////////////////////////////////
            // Test B: Push the track 2 regulator into the manager, and try to acquire from track 1 - should fail.
            ((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg2, null);
            TryAcquire(rsc, rrTrack1, false);
            TryAcquire(rsc, rrTrack2, true);

            /////////////////////////////////////////////////////////////////////////////
            // Test C: Push the track 1 regulator into the manager, and try to acquire from track 1 - should succeed.
            ((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg1, null);
            TryAcquire(rsc, rrTrack2, false);
            TryAcquire(rsc, rrTrack1, true);

            /////////////////////////////////////////////////////////////////////////////
            // Test D: Try to acquire from kahuna - should succeed. Then release, pop access reg, try again, should still succeed.
            TryAcquire(rsc, rrKahuna, true);
            ((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
            TryAcquire(rsc, rrKahuna, true);


            /////////////////////////////////////////////////////////////////////////////
            // Test E: Clear all access regulators, try with each key to acquire. All should succeed.
            ((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
            ((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
            TryAcquire(rsc, rrTrack1, true);
            TryAcquire(rsc, rrTrack2, true);
            TryAcquire(rsc, rrKahuna, true);

        }

        private void TryAcquire(SelfManagingResource rsc, IResourceRequest irr, bool expectSuccess)
        {

            string result = "Acqusition of " + rsc.Name + " using key " + irr.Key.ToString() + " expected to " + (expectSuccess ? "succeed" : "fail") + ".";
            if (rsc.Acquire(irr, false) == expectSuccess)
            {
                if (expectSuccess)
                    rsc.Release(irr);
                Console.WriteLine("Sub-test passed : " + result);
            }
            else
            {
                Assert.Fail("Sub-test failed : " + result);
            }
        }


        private IResourceManager _resourcePoolForStarvation;
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Allocates and deallocates resources and checks the behavior")]
        public void TestStarvation()
        {

            Model model = new Model("Starvation Testing Model...");

            _resourcePoolForStarvation = new SelfManagingResource(model, "SteamSystem", Guid.NewGuid(), 1000.0, false, false, true);

            model.Starting += new ModelEvent(OnModelStarting);

            model.Start();

            foreach (IModelWarning warning in model.Warnings)
            {
                Console.WriteLine(warning.Name + " : " + warning.Narrative);
            }

        }
        private void OnModelStarting(IModel theModel)
        {
            theModel.Executive.RequestEvent(new ExecEventReceiver(GetScarceResource), DateTime.Now, 0.0, null, ExecEventType.Detachable);
            theModel.Executive.RequestEvent(new ExecEventReceiver(GetScarceResource), DateTime.Now, 1.0, null, ExecEventType.Detachable);
        }

        private void GetScarceResource(IExecutive exec, object userData)
        {
            _resourcePoolForStarvation.Acquire(new ResourceRequest(900.0), true);
        }


        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies ResourceManager.Add and Remove correctly update the resource pool — guards against ArrayList→List migration regressions.")]
        public void TestResourceManagerAddRemoveAndCount()
        {
            Model model = new Model("RM Test Model");
            ResourceManager rm = new ResourceManager(model, "TestPool", Guid.NewGuid());

            Resource rsc1 = new Resource(model, "Resource A", Guid.NewGuid(), 1.0, 1.0, true, true, true);
            Resource rsc2 = new Resource(model, "Resource B", Guid.NewGuid(), 1.0, 1.0, true, true, true);
            Resource rsc3 = new Resource(model, "Resource C", Guid.NewGuid(), 1.0, 1.0, true, true, true);

            Assert.Empty(rm.Resources);

            rm.Add(rsc1);
            rm.Add(rsc2);
            Assert.Equal(2, rm.Resources.Count);

            rm.Add(rsc3);
            Assert.Equal(3, rm.Resources.Count);

            rm.Remove(rsc2);
            Assert.Equal(2, rm.Resources.Count);
            Assert.DoesNotContain(rsc2, rm.Resources);
            Assert.Contains(rsc1, rm.Resources);
            Assert.Contains(rsc3, rm.Resources);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies ResourceManager resources can be enumerated via foreach and looked up by Guid — guards against ArrayList→List migration regressions.")]
        public void TestResourceManagerEnumeration()
        {
            Model model = new Model("RM Enum Test Model");
            ResourceManager rm = new ResourceManager(model, "EnumPool", Guid.NewGuid());

            Guid guid0 = Guid.NewGuid(), guid1 = Guid.NewGuid(), guid2 = Guid.NewGuid();
            rm.Add(new Resource(model, "R1", guid0, 1.0, 1.0, true, true, true));
            rm.Add(new Resource(model, "R2", guid1, 1.0, 1.0, true, true, true));
            rm.Add(new Resource(model, "R3", guid2, 1.0, 1.0, true, true, true));

            Assert.Equal(3, rm.Resources.Count);

            // Verify foreach enumeration (IEnumerable path)
            int count = 0;
            foreach (IResource r in rm)
            {
                Assert.NotNull(r);
                count++;
            }
            Assert.Equal(3, count);

            // Verify Guid-based indexer
            Assert.NotNull(rm[guid0]);
            Assert.NotNull(rm[guid1]);
            Assert.NotNull(rm[guid2]);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies ResourceManager add/remove/clear keep Manager links and events consistent — guards resource-pool regression work.")]
        public void TestResourceManagerManagerLinksAndLifecycleEvents()
        {
            Model model = new Model("RM Lifecycle Test Model");
            ResourceManager rm = new ResourceManager(model, "LifecyclePool", Guid.NewGuid());
            Resource rsc1 = new Resource(model, "Resource A", Guid.NewGuid(), 1.0, 1.0, true, true, true);
            Resource rsc2 = new Resource(model, "Resource B", Guid.NewGuid(), 1.0, 1.0, true, true, true);
            int addedCount = 0;
            int removedCount = 0;

            rm.ResourceAdded += delegate (IResourceManager _, IResource __) { addedCount++; };
            rm.ResourceRemoved += delegate (IResourceManager _, IResource __) { removedCount++; };

            rm.Add(rsc1);
            rm.Add(rsc2);

            Assert.Same(rm, rsc1.Manager);
            Assert.Same(rm, rsc2.Manager);
            Assert.Equal(2, addedCount);

            rm.Remove(rsc1);

            Assert.Null(rsc1.Manager);
            Assert.Same(rm, rsc2.Manager);
            Assert.Equal(1, removedCount);
            Assert.Single(rm.Resources);

            rm.Clear();

            Assert.Empty(rm.Resources);
            Assert.Null(rsc2.Manager);
            Assert.Equal(2, removedCount);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 3: Verifies the resource-manager public collection surface is typed/read-only and still exposes the expected live members.")]
        public void TestResourceManagerApiCollectionsAreTypedAndReadOnly()
        {
            Assert.Equal(typeof(IReadOnlyList<IResource>), typeof(IResourceManager).GetProperty(nameof(IResourceManager.Resources))!.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<IResource>), typeof(ResourceManager).GetProperty(nameof(ResourceManager.Resources))!.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<IResource>), typeof(SelfManagingResource).GetProperty(nameof(SelfManagingResource.Resources))!.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<IResource>), typeof(MaterialResourceItem).GetProperty(nameof(MaterialResourceItem.Resources))!.PropertyType);
            Assert.Equal(typeof(IReadOnlyCollection<IResourceManager>), typeof(IResourceManagerCollection).GetMethod(nameof(IResourceManagerCollection.GetResourceManagers))!.ReturnType);
            Assert.Equal(typeof(IReadOnlyCollection<IResourceManager>), typeof(ResourceManagerCollection).GetMethod(nameof(ResourceManagerCollection.GetResourceManagers))!.ReturnType);

            Model model = new Model("RM API Shape Test Model");
            ResourceManager manager = new ResourceManager(model, "TypedPool", Guid.NewGuid());
            Resource resource = new Resource(model, "Resource A", Guid.NewGuid(), 1.0, 1.0, true, true, true);
            manager.Add(resource);

            IReadOnlyList<IResource> managerResources = manager.Resources;
            Assert.Single(managerResources);
            Assert.Same(resource, managerResources[0]);
            Assert.True(((ICollection<IResource>)managerResources).IsReadOnly);

            SelfManagingResource selfManaging = new SelfManagingResource(model, "SelfManaged", Guid.NewGuid(), 2.0, 2.0, true, true, true);
            IReadOnlyList<IResource> selfManagedResources = selfManaging.Resources;
            Assert.Single(selfManagedResources);
            Assert.Equal(selfManaging.Guid, selfManagedResources[0].Guid);
            Assert.True(((ICollection<IResource>)selfManagedResources).IsReadOnly);

            MaterialType materialType = new MaterialType(model, "Water", Guid.NewGuid(), 1.0, 1.0, MaterialState.Liquid, 18.0);
            MaterialResourceItem materialResource = new MaterialResourceItem(model, materialType, 5.0, 1.0, 10.0);
            IReadOnlyList<IResource> materialResources = materialResource.Resources;
            Assert.Single(materialResources);
            Assert.Same(materialResource, materialResources[0]);
            Assert.True(((ICollection<IResource>)materialResources).IsReadOnly);

            ResourceManagerCollection collection = new ResourceManagerCollection();
            collection.Add(manager);
            collection.Add(selfManaging);

            IReadOnlyCollection<IResourceManager> managers = collection.GetResourceManagers();
            Assert.Equal(2, managers.Count);
            Assert.Contains(manager, managers);
            Assert.Contains(selfManaging, managers);
            Assert.True(((ICollection<IResourceManager>)managers).IsReadOnly);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies ResourceManagerCollection add/remove/lookup semantics and lifecycle events without locking its collection return shape.")]
        public void TestResourceManagerCollectionLifecycleAndLookup()
        {
            Model model = new Model("RM Collection Test Model");
            ResourceManagerCollection collection = new ResourceManagerCollection();
            ResourceManager rm1 = new ResourceManager(model, "Pool A", Guid.NewGuid());
            ResourceManager rm2 = new ResourceManager(model, "Pool B", Guid.NewGuid());
            int addedCount = 0;
            int removedCount = 0;
            object addedSubject = null;
            object removedSubject = null;
            IResourceManager lastAdded = null;
            IResourceManager lastRemoved = null;

            collection.ResourceManagerAdded += delegate(object subject, IResourceManager manager)
            {
                addedCount++;
                addedSubject = subject;
                lastAdded = manager;
            };
            collection.ResourceManagerRemoved += delegate(object subject, IResourceManager manager)
            {
                removedCount++;
                removedSubject = subject;
                lastRemoved = manager;
            };

            collection.Add(rm1);
            collection.Add(rm2);

            Assert.Equal(2, addedCount);
            Assert.Same(collection, addedSubject);
            Assert.Same(rm2, lastAdded);
            Assert.Same(rm1, collection.GetResourceManager(rm1.Guid));
            Assert.Same(rm2, collection.GetResourceManager(rm2.Guid));
            Assert.Null(collection.GetResourceManager(Guid.NewGuid()));

            int managersSeen = 0;
            bool sawRm1 = false;
            bool sawRm2 = false;
            foreach (IResourceManager manager in collection.GetResourceManagers())
            {
                managersSeen++;
                sawRm1 |= ReferenceEquals(manager, rm1);
                sawRm2 |= ReferenceEquals(manager, rm2);
            }

            Assert.Equal(2, managersSeen);
            Assert.True(sawRm1);
            Assert.True(sawRm2);

            collection.Remove(rm1);

            Assert.Equal(1, removedCount);
            Assert.Same(collection, removedSubject);
            Assert.Same(rm1, lastRemoved);
            Assert.Null(collection.GetResourceManager(rm1.Guid));
            Assert.Same(rm2, collection.GetResourceManager(rm2.Guid));

            managersSeen = 0;
            sawRm1 = false;
            sawRm2 = false;
            foreach (IResourceManager manager in collection.GetResourceManagers())
            {
                managersSeen++;
                sawRm1 |= ReferenceEquals(manager, rm1);
                sawRm2 |= ReferenceEquals(manager, rm2);
            }

            Assert.Equal(1, managersSeen);
            Assert.False(sawRm1);
            Assert.True(sawRm2);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies MultiKeyAccessRegulator honors matching keys and either-side subject equality — guards earmarking behavior.")]
        public void TestMultiKeyAccessRegulatorMatchesOnKeyAndSymmetricSubjectEquality()
        {
            object key = "Authorized";
            MultiKeyAccessRegulator regulator = new MultiKeyAccessRegulator("SharedSubject", new ArrayList(new object[] { key }));

            Assert.True(regulator.CanAcquire("SharedSubject", key));
            Assert.True(regulator.CanAcquire(new SubjectAlias("SharedSubject"), key));
            Assert.False(regulator.CanAcquire("SharedSubject", "Denied"));
            Assert.False(regulator.CanAcquire("OtherSubject", key));
            Assert.False(regulator.CanAcquire(null, key));
        }

        sealed class ResourceRequest : Highpoint.Sage.Resources.ResourceRequest
        {

            public ResourceRequest(double quantity) : base(quantity) { }

            public override double GetScore(IResource resource)
            {
                if (resource.Available >= QuantityDesired)
                    return double.MaxValue;
                return Double.MinValue;
            }

            protected override ResourceRequestSource GetDefaultReplicator()
            {
                return new ResourceRequestSource(DefaultReplicator);
            }

            private ResourceRequest DefaultReplicator()
            {
                ResourceRequest irr = new ResourceRequest(QuantityDesired);
                irr.DefaultResourceManager = DefaultResourceManager;
                return irr;
            }

        }

        sealed class SubjectAlias
        {
            private readonly string _subjectName;

            public SubjectAlias(string subjectName)
            {
                _subjectName = subjectName;
            }

            public override bool Equals(object obj)
            {
                return obj is string candidate && candidate.Equals(_subjectName, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                return _subjectName.GetHashCode(StringComparison.Ordinal);
            }
        }

    }
}


