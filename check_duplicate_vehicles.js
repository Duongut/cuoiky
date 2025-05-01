// Script to find duplicate vehicles with vehicleId M001
db = db.getSiblingDB('SmartParkingDb');
var duplicates = db.Vehicles.find({vehicleId: "M001"}).toArray();
print("Found " + duplicates.length + " vehicles with vehicleId M001");
duplicates.forEach(function(vehicle, index) {
    print("Vehicle " + (index + 1) + ":");
    printjson(vehicle);
});
