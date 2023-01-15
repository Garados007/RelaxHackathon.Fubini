# RelaxHackathon.Fubini

> This project was part of a Hackathon and will no longer be maintained. If you think you can extend it feel free to create a fork

This project was created in the Relaxdays Code Challenge Vol. 1. See https://sites.google.com/relaxdays.de/hackathon-relaxdays/startseite for more information. 
My participant ID in the challenge was: CC-VOL1-49

## How to run this project

You can get a running version of this code by using:

```bash
git clone https://github.com/Garados007/RelaxHackathon.Fubini.git
cd RelaxHackathon.Fubini
docker build -t fubini .
docker run -it fubini 3500
```

The output will be formated json that will be printed to the std out. The numbers are not (!) converted to strings. They are kept as they are.

This code will be build for x64 processors.

If you dont want any output and just measure the time, call this:

```bash
docker run -it fubini --no-out 3500
```
