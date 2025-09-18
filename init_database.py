from pymongo import MongoClient
import sys

def initialize_database():
    """Initialize the MongoDB database with parking slots"""
    print("Initializing MongoDB database...")

    # Connect to MongoDB
    try:
        client = MongoClient("mongodb://localhost:27017/")
        print("Connected to MongoDB")
    except Exception as e:
        print(f"Could not connect to MongoDB: {e}")
        return False

    # 🧹 Drop the entire database (automatically without confirmation)
    print("Dropping the entire SmartParkingDb...")
    client.drop_database("SmartParkingDb")
    print("Dropped existing SmartParkingDb")

    # Reconnect to fresh database
    db = client["SmartParkingDb"]

    # Create ParkingSlots collection
    parking_slots = db["ParkingSlots"]

    # Create motorcycle slots (200)
    motorcycle_slots = []
    for i in range(1, 201):
        motorcycle_slots.append({
            "slotId": f"M{i:03d}",
            "type": "MOTORBIKE",
            "status": "AVAILABLE",
            "currentVehicleId": None
        })

    # Create car slots (50)
    car_slots = []
    for i in range(1, 51):
        car_slots.append({
            "slotId": f"C{i:03d}",
            "type": "CAR",
            "status": "AVAILABLE",
            "currentVehicleId": None
        })

    # Insert all slots
    if motorcycle_slots:
        parking_slots.insert_many(motorcycle_slots)
        print(f"Created {len(motorcycle_slots)} motorcycle parking slots")
    
    if car_slots:
        parking_slots.insert_many(car_slots)
        print(f"Created {len(car_slots)} car parking slots")
    
    # Show summary
    print("\nDatabase initialization complete!")
    print(f"Total parking slots: {len(motorcycle_slots) + len(car_slots)}")
    print(f"- Motorcycle slots: {len(motorcycle_slots)}")
    print(f"- Car slots: {len(car_slots)}")
    
    return True

if __name__ == "__main__":
    print("Starting database reset and initialization...")
    initialize_database()
    print("Process completed.")
