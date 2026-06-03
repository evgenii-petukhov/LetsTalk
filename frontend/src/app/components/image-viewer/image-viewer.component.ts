import {
    Component,
    HostBinding,
    HostListener,
    inject,
    OnDestroy,
    OnInit,
    signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { IImageDto } from '../../api-client/api-client';
import { errorMessages } from '../../constants/errors';
import { ErrorService } from '../../services/error.service';
import { StoreService } from '../../services/store.service';
import { Location } from '@angular/common';
import { filter, Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-image-viewer',
    templateUrl: './image-viewer.component.html',
    styleUrls: ['./image-viewer.component.scss'],
    standalone: false,
})
export class ImageViewerComponent implements OnInit, OnDestroy {
    backgroundImage = signal<string>('');
    isVisible = signal<boolean>(false);
    
    @HostBinding('class.visible') 
    get visible() { 
        return this.isVisible(); 
    }

    private imageKey: IImageDto;
    
    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly storeService = inject(StoreService);
    private readonly errorService = inject(ErrorService);
    private readonly route = inject(ActivatedRoute);
    private readonly location = inject(Location);

    @HostListener('click')
    close(): void {
        this.resetViewer();
    }

    ngOnInit(): void {
        this.route.params
            .pipe(filter(params => params && params['imageKey']), takeUntil(this.unsubscribe$))
            .subscribe(async (params) => {
                const [id, fileStorageTypeId] = params['imageKey'].split('_');
                this.imageKey = {
                    id,
                    fileStorageTypeId: Number(fileStorageTypeId),
                };

                await this.loadImage();
            });
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    private async loadImage(): Promise<void> {
        try {
            const image = await this.storeService.getImageContent(
                this.imageKey,
            );
            this.backgroundImage.set(image.content);
            this.isVisible.set(true);
        } catch (error) {
            this.errorService.handleError(error, errorMessages.downloadImage);
            this.resetViewer();
        }
    }

    private resetViewer(): void {
        this.backgroundImage.set('');
        this.isVisible.set(false);
        this.location.back();
    }
}
