// Connect to the database
db = db.getSiblingDB('SmartParkingDb');

// Find all vehicles with vehicleId M001
var vehicles = db.Vehicles.find({vehicleId: "M001"}).toArray();
print("Found " + vehicles.length + " vehicles with vehicleId M001");

// If there are multiple vehicles with the same ID, keep only the newest one
if (vehicles.length > 1) {
    // Sort by createdAt (newest first)
    vehicles.sort(function(a, b) {
        var aDate = a.createdAt ? new Date(a.createdAt) : new Date(0);
        var bDate = b.createdAt ? new Date(b.createdAt) : new Date(0);
        return bDate - aDate;
    });
    
    // Keep the most recent one
    var keepVehicle = vehicles[0];
    print("Keeping the most recent vehicle with _id: " + keepVehicle._id);
    
    // Delete the others
    for (var i = 1; i < vehicles.length; i++) {
        print("Deleting vehicle with _id: " + vehicles[i]._id);
        db.Vehicles.deleteOne({_id: vehicles[i]._id});
    }
    
    print("Deleted " + (vehicles.length - 1) + " duplicate vehicles");
} else {
    print("No duplicates found, nothing to delete");
}

// Verify the result
var remainingVehicles = db.Vehicles.find({vehicleId: "M001"}).toArray();
print("After cleanup: Found " + remainingVehicles.length + " vehicles with vehicleId M001");
