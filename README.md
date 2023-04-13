# Cynox.ModControl
ModControl is a proprietary communication protocol that is used by several Cynox devices like Camp-Control and Camp-Control Compact.

This library provides a high level abstraction of the protocol as intended for use by the master. The master always initiates the communication and it addresses a specific client by using the respective client/device address.

## Quick start example

* Connect to device
* Send requests to switch an output and retrieve a counter value
* Handle responses and errors

```c#
// Create default device
var device = new ModControlDevice {
    Address = 1,
};

// Establish connection
bool success = device.Connect(new TcpConnection("192.168.6.155", 1470));

if (success)
{
    var setOutputResponse = device.SetOutput(0, true);

    // Check if request was successful
    if (setOutputResponse.Error != ResponseError.None)
    {
        Debug.WriteLine($"Request failed: {setOutputResponse.Error}");
    }

    var getCounterResponse = device.GetCounter(2);

    // Check if request was successful
    if (getCounterResponse.Error != ResponseError.None)
    {
        Debug.WriteLine($"Request failed: {getCounterResponse.Error}");
    }
    else
    {
        // Log counter value
        Debug.WriteLine($"Current counter value for channel {getCounterResponse.Channel} = {getCounterResponse.Value}");
    }
}

// Disconnect
device.Disconnect();
```
