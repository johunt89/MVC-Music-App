using Microsoft.EntityFrameworkCore;
using MVC_Music.Models;
using System.Numerics;
using MVC_Music.ViewModels;

namespace MVC_Music.Data
{
    public class MusicContext : DbContext
    {
        public MusicContext(DbContextOptions<MusicContext> options)
            : base(options)
        {
        }

        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<Musician> Musicians { get; set; }
        public DbSet<Play> Plays { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Performance> Performances { get; set; }
        public DbSet<MusicianPhoto> MusicianPhotos { get; set; }
        public DbSet<MusicianThumbnail> MusicianThumbnails { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<MusicianDocument> MusicianDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            //Many to Many Primary Key
            modelBuilder.Entity<Play>()
            .HasKey(p => new { p.MusicianID, p.InstrumentID });

            //Add a unique index to the Musician SIN
            modelBuilder.Entity<Musician>()
            .HasIndex(p => p.SIN)
            .IsUnique();

            //NOTE: EACH OF THE FOLLOWING DELETE RESTRICTIONS
            //      CAN BE WRITTEN TWO WAYS: 
            //          FROM THE PARENT TABLE PERSPECTIVE OR
            //          FROM THE CHILD TABLE PERSPECTIVE


            //Prevent Cascade Delete from Instrument to Musician (Parent Perspective)
            modelBuilder.Entity<Instrument>()
                .HasMany<Musician>(p => p.Musicians)
                .WithOne(c => c.Instrument)
                .HasForeignKey(c => c.InstrumentID)
                .OnDelete(DeleteBehavior.Restrict);
            //Prevent Cascade Delete from Instrument to Musician (Child Perspective)
            //modelBuilder.Entity<Musician>()
            //    .HasOne(c => c.Instrument)
            //    .WithMany(p => p.Musicians)
            //    .HasForeignKey(c => c.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            //Prevent Cascade Delete from Instrument to Play (Parent Perspective)
            modelBuilder.Entity<Instrument>()
                .HasMany<Play>(p => p.Plays)
                .WithOne(c => c.Instrument)
                .HasForeignKey(c => c.InstrumentID)
                .OnDelete(DeleteBehavior.Restrict);
            //Prevent Cascade Delete from Instrument to Play (Child Perspective)
            //modelBuilder.Entity<Plays>()
            //    .HasOne(c => c.Instrument)
            //    .WithMany(p => p.Plays)
            //    .HasForeignKey(c => c.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Genre>()
                .HasMany<Album>(a => a.Albums)
                .WithOne(g => g.Genre)
                .HasForeignKey(g => g.GenreID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Genre>()
                .HasMany<Song>(s => s.Songs)
                .WithOne(g => g.Genre)
                .HasForeignKey(g => g.GenreID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Album>()
                .HasMany<Song>(s => s.Songs)
                .WithOne(a => a.Album)
                .HasForeignKey(a => a.AlbumID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Song>()
                .HasMany<Performance>(p => p.Performances)
                .WithOne(s => s.Song)
                .HasForeignKey(s => s.SongID);

            modelBuilder.Entity<Instrument>()
                .HasMany<Performance>(p => p.Performances)
                .WithOne(i => i.Instrument)
                .HasForeignKey(i => i.InstrumentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Musician>()
                .HasMany<Performance>(p => p.Performances)
                .WithOne(m => m.Musician)
                .HasForeignKey(m => m.MusicianID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Performance>()
                .HasIndex(i => new { i.MusicianID, i.InstrumentID, i.SongID })
                .IsUnique();

            modelBuilder.Entity<Musician>()
                .HasMany<MusicianDocument>(m => m.MusicianDocuments)
                .WithOne(g => g.Musician)
                .HasForeignKey(g => g.MusicianID);

        }

        public DbSet<MVC_Music.ViewModels.PerformanceSummaryVM> PerformanceSummaryVM { get; set; }

    }
}
