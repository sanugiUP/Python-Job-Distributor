# Project Description
## Overview
This repository contains a fully functioning Peer-to-Peer (P2P) application built on the .NET Framework. The application facilitates the distribution and execution of Python jobs among connected peers within a network.

## Features
### P2P Architecture
> Decentralized Network: Peers can join the network dynamically and collaborate in the job execution process.

> Communication: Utilizes .NET's networking capabilities for peer discovery, job distribution, and result retrieval.

### Job Pool Management
> Job Submission: Peers can submit Python jobs to the job pool for execution.

> Job Execution: Peers pick up available jobs from the pool for execution based on availability and system resources.

### Python Job Execution
> Python Integration: The application enables the execution of Python scripts distributed among connected peers.

> Result Aggregation: Peers collect and transmit job results back to the originating peer for consolidation.

### Graphical User Interface (GUI)
> User Interaction: Provides a user-friendly GUI using WPF (Windows Presentation Foundation) for interaction and job monitoring.

> Job Status: Displays job queue, execution progress, and results for better visibility.

## Purpose
The Peer-to-Peer Python Job Execution System aims to harness the collective computational power of connected peers for executing Python jobs. It serves as a distributed computing platform where peers collaborate in job execution, optimizing resource utilization and job completion times.

## Usage
To run and test the application:

> Create a new WPF App (.NET Framework) project in Visual Studio.

> Replace the default .xaml and .cs files with the provided code files from this repository for ClientOne and ClientTwo.

> Compile and run the application on multiple instances to simulate the P2P network.

> Use the GUI to submit, monitor, and observe the execution of Python jobs among connected peers.
