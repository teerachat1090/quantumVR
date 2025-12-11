import json

def test_connection():
    num = 1+1
    data = {
        "number":num,
        "status": "success",
        "message": "Python script is running and connected!"
    }
    print(json.dumps(data))
if __name__ == "__main__":
    test_connection()