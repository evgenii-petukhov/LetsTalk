import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageComponent } from './message.component';
import { Message } from '../../../models/message';
import { ImagePreview } from '../../../models/image-preview';
import { By } from '@angular/platform-browser';
import { ImageStubComponent } from '../image/image.component.stub';
import { IImageDto, ImageDto } from '../../../api-client/api-client';
import { beforeEach, describe, expect, it } from 'vitest';

describe('MessageComponent', () => {
    let component: MessageComponent;
    let fixture: ComponentFixture<MessageComponent>;

    const imageKey: IImageDto = {
        id: 'image-id',
        fileStorageTypeId: 1,
    };

    const image = new ImageDto(imageKey);

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [MessageComponent, ImageStubComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(MessageComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should apply "btm-right" class when message.isMine is true', () => {
        // Arrange
        component.message = new Message({ isMine: true });

        // Act
        fixture.detectChanges();

        // Assert
        const bubbleElement = fixture.debugElement.query(By.css('.bubble'));
        expect(bubbleElement.classes['btm-right']).toBe(true);
    });

    it('should apply "btm-left" class when message.isMine is false', () => {
        // Arrange
        component.message = new Message({ isMine: false });

        // Act
        fixture.detectChanges();

        // Assert
        const bubbleElement = fixture.debugElement.query(By.css('.bubble'));
        expect(bubbleElement.classes['btm-left']).toBe(true);
    });

    it('should apply "extra-padding" class when message.textHtml is present and message.isMine is false or message.linkPreview is not present', () => {
        // Arrange
        component.message = new Message({
            textHtml: '<p>Some HTML</p>',
            isMine: false,
        });

        // Act
        fixture.detectChanges();

        // Assert
        const bubbleElement = fixture.debugElement.query(By.css('.bubble'));
        expect(bubbleElement.classes['extra-padding']).toBe(true);
    });

    it('should not apply "extra-padding" class when message.linkPreview is present', () => {
        // Arrange
        component.message = new Message({
            linkPreview: {
                url: 'https://example.com',
                imageUrl: 'https://example.com/image.png',
                title: 'Example',
            },
        });

        // Act
        fixture.detectChanges();

        // Assert
        const bubbleElement = fixture.debugElement.query(
            By.css('.bubble'),
        ).nativeElement;
        const hasExtraPadding =
            bubbleElement.classList.contains('extra-padding');
        expect(hasExtraPadding).toBeFalsy();
    });

    it('should display link preview with image when linkPreview.imageUrl is provided and no image error', () => {
        // Arrange
        component.message = new Message({
            linkPreview: {
                url: 'http://example.com',
                imageUrl: 'http://example.com/image.jpg',
                title: 'Example',
            },
        });

        // Act
        fixture.detectChanges();

        // Assert
        const linkPreviewImage = fixture.debugElement.query(
            By.css('.link-preview-image img'),
        );
        expect(linkPreviewImage).toBeTruthy();
        expect(linkPreviewImage.nativeElement.src).toBe(
            'http://example.com/image.jpg',
        );
    });

    it('should not display link preview image when isImageError is true', () => {
        // Arrange
        component.message = new Message({
            linkPreview: { imageUrl: 'http://example.com/image.jpg' },
        });
        component.isImageError = true;

        // Act
        fixture.detectChanges();

        // Assert
        const linkPreviewImage = fixture.debugElement.query(
            By.css('.link-preview-image img'),
        );
        expect(linkPreviewImage).toBeFalsy();
    });

    it('should display link preview text', () => {
        // Arrange
        component.message = new Message({
            linkPreview: { url: 'http://example.com', title: 'Example' },
        });

        // Act
        fixture.detectChanges();

        // Assert
        const linkPreviewText = fixture.debugElement.query(
            By.css('.link-preview-text'),
        ).nativeElement;
        expect(linkPreviewText.textContent).toContain('Example');
    });

    it('should display HTML content when message.textHtml is provided', () => {
        // Arrange
        component.message = new Message({ textHtml: '<b>Bold Text</b>' });

        // Act
        fixture.detectChanges();

        // Assert
        const contentElement = fixture.debugElement.query(
            By.css('.content'),
        ).nativeElement;
        expect(contentElement.innerHTML).toContain('<b>Bold Text</b>');
    });

    it('should display image when message.image is provided', () => {
        // Arrange
        component.message = new Message({
            image: image,
            imagePreview: new ImagePreview({
                id: '1',
                width: 100,
                height: 100,
            }),
        });

        // Act
        fixture.detectChanges();

        // Assert
        const imageElement = fixture.debugElement.query(By.css('app-image'));
        expect(imageElement).toBeTruthy();
    });

    it('should display time formatted as HH:mm', () => {
        // Arrange
        const localDate = new Date(2023, 7, 23, 14, 34);
        component.message = new Message({ created: localDate });

        // Act
        fixture.detectChanges();

        // Assert
        const timeElement = fixture.debugElement.query(
            By.css('.time'),
        ).nativeElement;

        const hours = localDate.getHours().toString().padStart(2, '0');
        const minutes = localDate.getMinutes().toString().padStart(2, '0');
        const expectedTime = `${hours}:${minutes}`;

        expect(timeElement.textContent.trim()).toBe(expectedTime);
    });

    it('should set isImageError to true when onImageError is called', () => {
        // Arrange

        // Act
        component.onImageError();

        // Assert
        expect(component.isImageError).toBe(true);
    });
});
