// Script to fix duplicate vehicles with vehicleId M001
db = db.getSiblingDB('SmartParkingDb');

// Find all vehicles with vehicleId M001
var duplicates = db.Vehicles.find({vehicleId: "M001"}).toArray();
print("Found " + duplicates.length + " vehicles with vehicleId M001");

if (duplicates.length > 1) {
    // Sort by createdAt (newest first)
    duplicates.sort(function(a, b) {
        var aDate = a.createdAt ? new Date(a.createdAt) : new Date(0);
        var bDate = b.createdAt ? new Date(b.createdAt) : new Date(0);
        return bDate - aDate;
    });
    
    // Keep the most recent one
    var keepVehicle = duplicates[0];
    print("Keeping the most recent vehicle:");
    printjson(keepVehicle);
    
    // Remove the others
    for (var i = 1; i < duplicates.length; i++) {
        print("Removing duplicate vehicle " + i + ":");
        printjson(duplicates[i]);
        db.Vehicles.deleteOne({_id: duplicates[i]._id});
    }
    
    print("Duplicate vehicles removed successfully");
} else {
    print("No duplicates found, nothing to fix");
}

// Verify the fix
var remainingVehicles = db.Vehicles.find({vehicleId: "M001"}).toArray();
print("After fix: Found " + remainingVehicles.length + " vehicles with vehicleId M001");
if (remainingVehicles.length > 0) {
    print("Remaining vehicle:");
    printjson(remainingVehicles[0]);
}
