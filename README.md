# netcore-selenium-framework 
Bootstrapping of a bare minimum, opinionated .Net Core Selenium Framework using MsTest, the in-built .Net Core DI Container, Serilog, .runsettings and Visual Studio. 

My first goal is to be up and running with Selenium across several browsers within 15 minutes on either Linux or Windows in .Net Core using Visual Studio. By tweaking a few settings I want to optionally target Selenium Grid, different environments and change my control settings (such as timeouts and so forth). This repository lets me do that. 

My second goal is to have a personal reference showing patterns I can lift'n'shift into other frameworks (that I often encounter and have to maintain): browser selection, hot-reload, environment selection, timeouts/control management, superficial reporting, remote web driver configuration, environment variable overrides, multi-element eventual consistency, runsettings/IDE integration, simple logging, dependency injection/container initialization are included. 

If you are looking for a fully fledged Selenium / Automation framework implementation, that problem has already been solved by someone elsewhere: consider looking at [Atata Framework](https://github.com/atata-framework)

A few automated (raw, inline locator) tests are written against https://the-internet.herokuapp.com/ - that site contains all kinds of UI Automation Nastiness. 

NOTE: Appium is slowly being added, but is a work in progress. 

## Builds
A few example pipelines are included for reference. 

| Build | Result | Description |
| ----- | ------ | ----------- |
| internet.yml | [![Build Status](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_apis/build/status/netcore-selenium-framework-chrome-internet?branchName=master)](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_build/latest?definitionId=25&branchName=master) | Builds and runs the tests on a Windows VM and targets http://the-internet.herokuapp.com |
| internet-localhost.yml | [![Build Status](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_apis/build/status/netcore-selenium-framework-internet-localhost?branchName=master)](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_build/latest?definitionId=26&branchName=master) | Builds and runs the tests on an Ubuntu VM and targets 'the-internet' in a container started on the build system |
| zalenium-localhost-vm.yml | [![Build Status](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_apis/build/status/netcore-selenium-framework-zalenium-localhost-vm?branchName=master)](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_build/latest?definitionId=24&branchName=master) | Builds and runs the tests on an Ubuntu VM and targets http://the-internet.herokuapp.com via Zalenium (Dockerized Selenium Grid) started on the build system |
| netcore-selenium-framework-from-built-container | [![Build Status](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_apis/build/status/netcore-selenium-framework-from-built-container?branchName=master)](https://greyhamwoohoo.visualstudio.com/Public-Automation-Examples/_build/latest?definitionId=30&branchName=master) | Builds a container including both Google Chrome and the Test Binaries, runs the tests from the container targetting http://the-internet.herokuapp.com and then publishes test results.<br><br>Test results are 'dropped' onto the host using a mapped volume. | 

## Versions
The baseline framework is the same - but the attachable driver / hot-reload workflow implementation is very dependent on the version of Selenium in use. 

| Folder | Description | 
| ------ | ----------- |
| src/NetCore-Selenium | Reference Implementation: <br><br>Baseline framework and attachable drivers using Selenium 3.141  (IE, Chrome, Edge) |
| src/NetCore-Selenium-4.0-alpha3 | (OBSOLETE) Baseline framework and attachable drivers using Selenium 4.0.0.0-alpha-03  (IE, Chrome, Edge) <br><br>NOTE: This was created as a Spike, it works, but is out of date from the original implementation. NetCore-Selenium is the reference implementation. |

## Infrastructure
You can run 'the-internet' yourself in the following ways: 

| Target | Description |
| ------ | ----------- |
| infra/docker-localhost | Uses a docker-compose file to spin up the-internet locally |
| infra/minikube | Uses minikube / kubectl to start the-internet locally and expose it via an external 'LoadBalancer' port on your own machine |
| infra/zalenium-docker-localhost | Runs Zalenium (Dockerized Selenium Grid) - supports Firefox and Chrome browsers |
