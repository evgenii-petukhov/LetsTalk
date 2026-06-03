import {
    Component,
    computed,
    inject,
    OnDestroy,
    OnInit,
    signal,
} from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { StoreService } from '../../services/store.service';
import { ApiService } from '../../services/api.service';
import { faUpload } from '@fortawesome/free-solid-svg-icons';
import { selectLoggedInUser } from '../../state/logged-in-user/logged-in-user.selectors';
import { Store } from '@ngrx/store';
import { environment } from '../../../environments/environment';
import { ImageRoles, UploadImageResponse } from '../../protos/file_upload_pb';
import { ErrorService } from '../../services/error.service';
import { errorMessages } from '../../constants/errors';
import { ImageUploadService } from '../../services/image-upload.service';
import { Location } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';

// https://angular.io/guide/reactive-forms
// https://angular.io/guide/form-validation
// https://www.pluralsight.com/guides/how-to-display-validation-messages-using-angular
// https://nils-mehlhorn.de/posts/angular-navigate-back-previous-page/
@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.scss'],
    standalone: false,
})
export class ProfileComponent implements OnInit, OnDestroy {
    isSending = signal(false);
    email = signal('');
    selectedPhotoUrl = signal<string | null>(null);

    private readonly fb = inject(FormBuilder);
    private readonly storeService = inject(StoreService);
    private readonly apiService = inject(ApiService);
    private readonly store = inject(Store);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly errorService = inject(ErrorService);
    private readonly location = inject(Location);

    private loggedInUser = toSignal(this.store.select(selectLoggedInUser), {
        initialValue: null,
    });
    urlOptions = computed(() => {
        const loggedInUser = this.loggedInUser();
        return [
            this.selectedPhotoUrl(),
            loggedInUser?.image,
            loggedInUser?.photoUrl,
        ];
    });
    form = this.fb.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        photoUrl: [null],
    });
    faUpload = faUpload;

    async ngOnInit(): Promise<void> {
        const account = await this.storeService.getLoggedInUser();

        this.form.setValue({
            firstName: account.firstName,
            lastName: account.lastName,
            photoUrl: null,
        });
        this.email.set(account.email);
    }

    ngOnDestroy(): void {
        const photoUrl = this.selectedPhotoUrl();
        if (photoUrl) {
            URL.revokeObjectURL(photoUrl);
        }
    }

    async onSubmit(): Promise<void> {
        this.isSending.set(true);
        const sizeLimits = environment.imageSettings.limits.avatar;
        try {
            const photoUrl = this.selectedPhotoUrl();
            const image = photoUrl
                ? await this.imageUploadService.resizeAndUploadImage(
                      photoUrl,
                      sizeLimits.width,
                      sizeLimits.height,
                      ImageRoles.AVATAR,
                  )
                : null;
            await this.submitForm(image);
        } catch (e) {
            this.handleSubmitError(e, errorMessages.uploadImage);
        }
    }

    onBack(): void {
        this.location.back();
    }

    async onAvatarSelected(event: Event): Promise<void> {
        const files = (event.target as HTMLInputElement).files;

        if (files && files.length) {
            const buffer = await files[0].arrayBuffer();
            const base64 = URL.createObjectURL(new Blob([buffer]));
            this.form.patchValue({
                photoUrl: base64,
            });
            this.selectedPhotoUrl.set(base64);
        }
    }

    private async submitForm(image: UploadImageResponse): Promise<void> {
        try {
            const profileDto = await this.apiService.saveProfile(
                this.form.value.firstName,
                this.form.value.lastName,
                image,
            );
            this.storeService.setLoggedInUser(profileDto);
            this.location.back();
            this.isSending.set(false);
        } catch (e) {
            this.handleSubmitError(e, errorMessages.saveProfile);
        }
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private handleSubmitError(e: any, defaultMessage: string) {
        this.errorService.handleError(e, defaultMessage);
        this.isSending.set(false);
    }
}
