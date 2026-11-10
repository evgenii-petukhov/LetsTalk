import { inject, Injectable } from '@angular/core';
import {
    HttpTransportType,
    HubConnectionBuilder,
    LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import {
    IImagePreviewDto,
    ILinkPreviewDto,
    IMessageDto,
} from '../api-client/api-client';
import { ConstantRetryPolicy } from './constant-retry-policy';
import { SessionService } from './session.service';
import { IncomingCall } from '../models/incoming-call';
import { EstablishConnection } from '../models/establish-connection';

type TypeDtoMap = {
    MessageDto: IMessageDto;
    LinkPreviewDto: ILinkPreviewDto;
    ImagePreviewDto: IImagePreviewDto;
    IncomingCall: IncomingCall;
    EstablishConnection: EstablishConnection;
};

type TypeNames = keyof TypeDtoMap;
type TypeDto = TypeDtoMap[TypeNames];

@Injectable({
    providedIn: 'root',
})
export class SignalrService {
    private readonly sessionService = inject(SessionService);
    // The handshake carries the access cookie. Before the first reconnect attempt renew it if it may have expired.
    // A refused attempt shows that the guess was wrong or that the server is down: ask the server about the
    // session, which renews it when it has expired (the HTTP interceptor does that on the 401).
    private readonly retryPolicy = new ConstantRetryPolicy(
        environment.services.notifications.connectionInterval,
        (context) =>
            void (context.previousRetryCount === 0
                ? this.sessionService.ensureAccess()
                : this.sessionService.refresh()),
    );
    private readonly hubConnectionBuilder = new HubConnectionBuilder()
        .withUrl(environment.services.notifications.url, {
            skipNegotiation: true,
            transport: HttpTransportType.WebSockets,
            // the browser sends the HttpOnly session cookie with the WebSocket handshake
            withCredentials: true,
        })
        .withAutomaticReconnect(this.retryPolicy)
        .configureLogging(LogLevel.Information)
        .build();

    private isInitialized = false;
    private notificationEventName = 'SendNotificationAsync';
    private connectionTimerId!: number;
    private handlerMapping!: {
        [K in TypeNames]: (dto: TypeDtoMap[K]) => void;
    };

    constructor() {
        this.sessionService.sessionLost.subscribe(() => this.removeHandlers());
    }

    async init(
        messageHandler: (messageDto: IMessageDto) => Promise<void>,
        linkPreviewHandler: (response: ILinkPreviewDto) => void,
        imagePreviewHandler: (response: IImagePreviewDto) => void,
        incomingCallHandler: (response: IncomingCall) => void,
        establishConnectionHandler: (response: EstablishConnection) => void,
    ): Promise<void> {
        if (this.isInitialized) {
            return;
        }

        this.handlerMapping = {
            MessageDto: messageHandler,
            LinkPreviewDto: linkPreviewHandler,
            ImagePreviewDto: imagePreviewHandler,
            IncomingCall: incomingCallHandler,
            EstablishConnection: establishConnectionHandler,
        };

        const sessionActive = await this.setUpConnection();

        if (!this.isInitialized && sessionActive) {
            this.connectionTimerId = window.setInterval(async () => {
                if (!(await this.setUpConnection())) {
                    window.clearInterval(this.connectionTimerId);
                }
            }, environment.services.notifications.connectionInterval);
        }

        this.hubConnectionBuilder.onreconnected(() => {
            // the new handshake carries the session cookie again; nothing to re-send
            console.log('Notification service: reconnected');
        });
    }

    removeHandlers(): void {
        window.clearInterval(this.connectionTimerId);
        this.hubConnectionBuilder.off(this.notificationEventName);
        this.hubConnectionBuilder.stop();
        this.isInitialized = false;
    }

    // Resolves false when the session is over, so there is nothing to keep retrying
    private async setUpConnection(): Promise<boolean> {
        if (!(await this.sessionService.ensureAccess())) {
            return false;
        }

        try {
            await this.hubConnectionBuilder.start();
            this.isInitialized = true;
            window.clearInterval(this.connectionTimerId);
            this.hubConnectionBuilder.on(
                this.notificationEventName,
                (dto: TypeDto, typeName: TypeNames) => {
                    this.handlerMapping[typeName]?.(dto);
                },
            );
            console.log('Notification service: connected');
        } catch {
            console.log('Notification service: unable to establish connection');
            // the server refuses the handshake when the session is gone: find out, and
            // let SessionService drop the session (which stops this service) if so
            void this.sessionService.refresh();
        }

        return true;
    }
}
