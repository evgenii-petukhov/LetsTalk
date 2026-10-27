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
    private readonly retryPolicy = new ConstantRetryPolicy(
        environment.services.notifications.connectionInterval,
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

        await this.setUpConnection();

        if (!this.isInitialized) {
            this.connectionTimerId = window.setInterval(async () => {
                await this.setUpConnection();
            }, this.retryPolicy.nextRetryDelayInMilliseconds());
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

    private async setUpConnection(): Promise<void> {
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
    }
}
