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

```mermaid
flowchart TB
    classDef client fill:#dd0031,stroke:#a30022,color:#fff
    classDef svc fill:#e8f1fb,stroke:#2f6fb5,color:#0b2e55
    classDef broker fill:#fff4d6,stroke:#d19a00,color:#5a4200
    classDef db fill:#e6f4ea,stroke:#2e7d32,color:#143d17
    classDef aws fill:#fff0e0,stroke:#e07b00,color:#5c3000

    user(["Browser<br/><b>Angular SPA</b> · NgRx"]):::client

    subgraph services["Microservices (.NET 10)"]
        direction TB
        api["<b>Chat API</b><br/>REST · CQRS (MediatR)"]:::svc
        auth["<b>Authentication</b><br/>gRPC · JWT"]:::svc
        files["<b>File Storage</b><br/>gRPC"]:::svc
        notif["<b>Notifications</b><br/>SignalR hub"]:::svc
        img["<b>Image Processing</b><br/>SkiaSharp"]:::svc
        link["<b>Link Preview</b>"]:::svc
    end

    broker{{"Event broker<br/>Apache Kafka <i>or</i> AWS SNS/SQS<br/>(MassTransit)"}}:::broker

    subgraph data["Data stores"]
        direction LR
        sql[("MySQL<br/>EF Core")]:::db
        mongo[("MongoDB / Azure CosmosDB<br/>AWS DocumentDB")]:::db
        redis[("Redis")]:::db
    end

    subgraph aws["AWS"]
        direction LR
        s3[/"S3<br/>avatars & images"/]:::aws
        lambdaImg["Lambda<br/>image resize"]:::aws
        lambdaLink["Lambda<br/>link preview"]:::aws
    end

    %% Synchronous calls
    user -- "REST" --> api
    user -- "gRPC-web" --> files
    user <-. "SignalR (WebSocket)" .-> notif
    api -- "gRPC" --> auth
    files -- "gRPC" --> auth
    notif -- "gRPC" --> auth
    api --> sql & mongo & redis
    files --> s3

    %% Asynchronous events
    api == "publish events" ==> broker
    broker == "new message" ==> notif
    broker == "image resize" ==> img
    broker == "link preview" ==> link
    img --> lambdaImg
    link --> lambdaLink
    img -. "result (REST, background JWT)" .-> api
    link -. "result (REST, background JWT)" .-> api

    linkStyle 0,1,2,3,4,5,6,7,8,9 stroke:#2f6fb5
    linkStyle 10,11,12,13 stroke:#d19a00,stroke-width:3px
```

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