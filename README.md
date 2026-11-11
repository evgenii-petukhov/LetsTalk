# LetsTalk - Instant messenger 💬

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![MySQL](https://img.shields.io/badge/mysql-%2300f.svg?style=for-the-badge&logo=mysql&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-00684A?style=for-the-badge&logo=mongodb&logoColor=white)
![Apache Kafka](https://img.shields.io/badge/Apache%20Kafka-000?style=for-the-badge&logo=apachekafka)
![Redis](https://img.shields.io/badge/Redis-D82C20?style=for-the-badge&logo=redis&logoColor=white)
![Nginx](https://img.shields.io/badge/nginx-%23009639.svg?style=for-the-badge&logo=nginx&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white)
![Debian](https://img.shields.io/badge/Debian-A3002E?style=for-the-badge&logo=debian&logoColor=white)

![scheme](demo.gif)

🔴 [Live demo](https://letstalk.petukhov.fyi/)

## Description

LetsTalk is a messaging app for exchanging text messages, images, and video calls.

This project showcases my technical skills for potential employers and IT recruiters. It demonstrates that:

- I can:
  - Design event-driven system architectures
  - Work with cloud services such as AWS SNS/SQS, S3, Lambda, and DocumentDB
  - Build single-page web applications with Angular and TypeScript
  - Implement reactive state management with NgRx in Angular
  - Work with NoSQL databases such as MongoDB, Azure CosmosDB, and Redis
- I understand:
  - The OpenAPI Specification
  - Microservice communication patterns and protocols such as gRPC
  - Microservice architecture and event-driven development principles, with hands-on experience using Apache Kafka
  - Domain-driven design and data consistency ([see my article on DDD](https://www.linkedin.com/pulse/how-i-practiced-ddd-principles-ignoring-them-evgenii-petukhov/) on LinkedIn)

## Technical stack

**Back end**
- C#, .NET 10, ASP.NET Core
- Communication: REST, gRPC, SignalR, WebRTC (Cloudflare TURN)
- CQRS: MediatR
- Databases: MySQL (Entity Framework Core), MongoDB, Azure CosmosDB, and Redis
- Event messaging: Apache Kafka, AWS SNS/SQS (via MassTransit)
- AWS: S3, Lambda, SNS, SQS, CloudWatch
- Azure: AppInsights, CosmosDB

**Front end**
- TypeScript, Angular 21, NgRx
- Testing: Vitest

## Architecture

![scheme](scheme-compressed.svg)

The front end is an Angular single-page application using NgRx for reactive state management.

The back end follows a microservice, event-driven architecture. Depending on the configuration, it uses Apache Kafka or AWS SNS/SQS as the event broker. The table below describes each microservice:

| Microservice name | Protocol                    | Description                                                                                                                    |
| ----------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Chat API          | REST                        | Responsible for sending messages and account management                                                                        |
| Authentication    | gRPC                        | Generates and validates JSON Web Tokens                                                                                        |
| Notification      | Apache Kafka or AWS SNS/SQS | Sends notifications about new messages to the front-end via SignalR                                                            |
| Link preview      | Apache Kafka or AWS SNS/SQS | Decorates messages with a website's name and a picture preview in the cloud by calling AWS Lambda, if a message contains links |
| File storage      | gRPC                        | Saves avatars and images uploaded by users in the cloud (AWS S3) and serves them when requested                                |
| Image processing  | Apache Kafka or AWS SNS/SQS | Generates image previews in the cloud by calling AWS Lambda; uses [SkiaSharp](https://github.com/mono/SkiaSharp)               |

## Local deployment

The instructions below are for Windows.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (`RunAll.ps1` starts the `redis` and `kafka` containers)
- PowerShell

### Back end
1. Create the Redis and Kafka containers. They must be named `redis` and `kafka`, because `RunAll.ps1` starts them by name, and must expose the default ports (6379 for Redis, 9092 for Kafka):
   ```powershell
   docker run -d --name redis -p 6379:6379 redis
   docker run -d --name kafka -p 9092:9092 apache/kafka
   ```

2. Trust the ASP.NET Core development certificate (in an elevated PowerShell):
   ```powershell
   dotnet dev-certs https --trust
   ```

3. Create a system-level environment variable `LetsTalk_Server_FileStorage` pointing to the folder where uploaded files are stored (for example, `C:\LetsTalk.Server.FileStorage`). Restart your terminal or IDE afterwards so it picks up the variable.

4. Allow running local scripts in PowerShell:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

5. Start all services:
   ```powershell
   cd backend
   .\RunAll.ps1
   ```

### Front end
1. Install mkcert (in an elevated PowerShell):
   ```powershell
   winget install FiloSottile.mkcert
   ```

2. Install the local certificate authority:
   ```powershell
   mkcert -install
   ```

3. Generate a certificate for `127.0.0.1` and `localhost`:
   ```powershell
   cd frontend
   mkdir certs
   mkcert -cert-file certs/127.0.0.1.pem -key-file certs/127.0.0.1-key.pem 127.0.0.1 localhost
   ```

4. Install dependencies and start the development server:
   ```powershell
   npm install
   npm start
   ```

5. Open <https://127.0.0.1:4200>.