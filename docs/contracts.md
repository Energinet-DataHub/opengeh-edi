# Protocol Documentation

## Table of Contents

- [EnergySupplierChanged.proto](#EnergySupplierChanged.proto)
    - [EnergySupplierChanged](#.EnergySupplierChanged)
  
- [FutureEnergySupplierChangeAccepted.proto](#FutureEnergySupplierChangeAccepted.proto)
    - [FutureEnergySupplierChangeAccepted](#.FutureEnergySupplierChangeAccepted)
  
- [FutureEnergySupplierChangeCancelled.proto](#FutureEnergySupplierChangeCancelled.proto)
    - [FutureEnergySupplierChangeCancelled](#.FutureEnergySupplierChangeCancelled)

<a name="EnergySupplierChanged.proto"></a>

## EnergySupplierChanged.proto

<a name=".EnergySupplierChanged"></a>

### EnergySupplierChanged

This message is sent out when an energy supplier has been changed.

| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| MeteringPointId | [string](#string) |  | Unique metering point identification |
| EnergySupplierGLN | [string](#string) |  | Unique Energy Supplier identification. |
| EffectiveDate | [string](#string) |  | Date which the change of supplier goes into effect. |
| TransactionId | [string](#string) |  | Unique transaction ID of the process. Needed to effectuate the correct accepted process. |

<a name="FutureEnergySupplierChangeAccepted.proto"></a>

## FutureEnergySupplierChangeAccepted.proto

<a name=".FutureEnergySupplierChangeAccepted"></a>

### FutureEnergySupplierChangeAccepted

This message is sent out when a future energy supplier change is accepted. It informs who will be the future energy supplier of the respective metering point

| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| MeteringPointId | [string](#string) |  | Unique metering point identification |
| EnergySupplierGLN | [string](#string) |  | Unique Energy Supplier identification |
| EffectiveDate | [string](#string) |  | Date in which the supplier change will go into effect |
| TransactionId | [string](#string) |  | Unique transaction ID of the process. Needed to effectuate the correct process at a later date. |

<a name="FutureEnergySupplierChangeCancelled.proto"></a>

## FutureEnergySupplierChangeCancelled.proto

<a name=".FutureEnergySupplierChangeCancelled"></a>

### FutureEnergySupplierChangeCancelled

This message is sent out when an awaiting future change of energy supplier has been cancelled.

| Field | Type | Label | Description |
| ----- | ---- | ----- | ----------- |
| MeteringPointId | [string](#string) |  | Unique metering point identification |
| TransactionId | [string](#string) |  | Unique transaction ID of the process. Needed to cancel the correct process. |
