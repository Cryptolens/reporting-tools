# Floating License Audit Log

This script will return a history of how a license using the floating seat model is used over time,
summarizing average number of available seats (machine codes), etc.

By default, it will look at how the number of seats have changed over time 24 h back in time. Below is a sample report you can generate:

```
Max number of machines: 100
At this point, 100 seats are available.
Displaying history of events 5 days back in time.
2021-03-19 13:49:14     Activation      99      test
2021-03-19 13:49:35     Verification    99      test
2021-03-19 13:49:40     Activation      98      test4
2021-03-19 13:49:47     Deactivation    99      test4
2021-03-19 13:49:48     Deactivation    100     test
2021-03-23 09:47:48     Activation      99      device1
2021-03-23 09:47:52     Verification    99      device1
2021-03-23 09:47:54     Verification    99      device1
2021-03-23 09:47:57     Activation      98      device2
2021-03-23 09:48:01     Activation      97      device3
2021-03-23 09:49:34     Deactivation    98      device1
2021-03-23 09:49:37     Deactivation    99      device2
2021-03-23 09:49:41     Deactivation    100     device3
2021-03-26 12:56:09     Activation      99      deviceABC
2021-03-26 12:56:24     Deactivation    100     deviceABC
```

Please reach out to us at support@cryptolens.io if you have any questions or feedback.