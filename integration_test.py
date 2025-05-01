import requests
import os
import sys
import time

def test_license_plate_api():
    """Test the License Plate Recognition API"""
    print("Testing License Plate Recognition API...")
    
    # Check if the API is running
    try:
        response = requests.get("http://localhost:4050/health")
        if response.status_code == 200:
            print("✅ API is running")
        else:
            print("❌ API is not running properly")
            return False
    except requests.exceptions.ConnectionError:
        print("❌ Could not connect to the API. Make sure it's running on port 4050")
        return False
    
    # Test license plate recognition with a sample image
    test_image_path = "License-Plate-Recognition-main/test_image/3.jpg"
    if not os.path.exists(test_image_path):
        print(f"❌ Test image not found at {test_image_path}")
        return False
    
    try:
        with open(test_image_path, "rb") as img_file:
            files = {"image": img_file}
            response = requests.post("http://localhost:4050/recognize", files=files)
        
        if response.status_code == 200:
            result = response.json()
            if result.get("success"):
                print(f"✅ Successfully recognized license plate: {result.get('licensePlate')}")
                return True
            else:
                print(f"❌ API returned error: {result.get('error')}")
                return False
        else:
            print(f"❌ API returned status code {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Error testing license plate recognition: {e}")
        return False

def main():
    print("=== Integration Test for SmartParking System ===")
    
    # Test License Plate API
    if not test_license_plate_api():
        print("\n❌ License Plate API test failed. Please check the API and try again.")
        return
    
    print("\n✅ All tests passed!")
    print("\nNext steps:")
    print("1. Start the SmartParking.Core API")
    print("2. Test the full integration with a real image")
    print("3. Implement the frontend to visualize the parking system")

if __name__ == "__main__":
    main()
