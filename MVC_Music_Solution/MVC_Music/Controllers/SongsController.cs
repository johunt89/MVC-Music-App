using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Music.Data;
using MVC_Music.Models;
using MVC_Music.Utilities;

namespace MVC_Music.Controllers
{
    [Authorize]
    public class SongsController : Controller
    {
        private readonly MusicContext _context;

        public SongsController(MusicContext context)
        {
            _context = context;
        }

        // GET: Songs
        [Authorize(Roles = "Staff, Supervisor, Admin, User")]
        public async Task<IActionResult> Index(string SearchTitle, int? AlbumID, int? GenreID, int? page, string actionButton, int? pageSizeID, string sortDirection = "asc", string sortField = "Song")
        {
            ViewData["GenreID"] = new SelectList(_context
                    .Genres
                    .OrderBy(g => g.Name), "ID", "Name");

            ViewData["AlbumID"] = new SelectList(_context
                    .Albums
                    .OrderBy(a => a.Name), "ID", "Name");

            //sorting
            string[] sortOptions = new[] { "Title", "DateRecorded" };

            var songs = _context.Songs
                .Include(s => s.Album)
                .Include(s => s.Genre)
                .Include(p => p.Performances)
                .AsNoTracking();

            //filters
            if (GenreID.HasValue)
            {
                songs = songs.Where(g => g.GenreID == GenreID);
                ViewData["Filtering"] = " show";
            }
            if (AlbumID.HasValue)
            {
                songs = songs.Where(a => a.AlbumID == AlbumID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchTitle))
            {
                songs = songs.Where(p => p.Title.ToUpper().Contains(SearchTitle.ToUpper()));
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(actionButton)) 
            {
                page = 1;

                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField) 
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;
                }
            }

            if (sortField == "Title")
            {
                if (sortDirection == "asc")
                {
                    songs = songs
                        .OrderBy(s => s.Title)
                        .ThenBy(s => s.DateRecorded);
                }
                else
                {
                    songs = songs
                        .OrderByDescending(s => s.Title)
                        .ThenBy(s => s.DateRecorded);
                }
            }
            else if (sortField == "DateRecorded")
            {
                if (sortDirection == "asc")
                {
                    songs = songs
                        .OrderBy(s => s.DateRecorded)
                        .ThenBy(s => s.Title);
                }
                else
                {
                    songs = songs
                        .OrderByDescending(s => s.DateRecorded)
                        .ThenBy(s => s.Title);
                }
            }
            else //Sorting by Musician Name
            {
                if (sortDirection == "asc")
                {
                    songs = songs
                        .OrderBy(s => s.Album)
                        .ThenBy(s => s.Title);
                }
                else
                {
                    songs = songs
                        .OrderByDescending(s => s.Album)
                        .ThenBy(s => s.Title);
                }
            }
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "songs");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Song>.CreateAsync(songs.AsNoTracking(), page ?? 1, pageSize);
            
            return View(pagedData);
        }

        // GET: Songs/Details/5
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Songs == null)
            {
                return NotFound();
            }

            var song = await _context.Songs
                .Include(s => s.Album)
                .Include(s => s.Genre)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (song == null)
            {
                return NotFound();
            }

            return View(song);
        }

        // GET: Songs/Create
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public IActionResult Create()
        {
            ViewData["AlbumID"] = new SelectList(_context.Albums, "ID", "Name");
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name");
            return View();
        }

        // POST: Songs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Create([Bind("ID,Title,DateRecorded,GenreID,AlbumID")] Song song)
        {
            if (ModelState.IsValid)
            {
                _context.Add(song);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "SongPerformances", new { SongID = song.ID });
            }
            ViewData["AlbumID"] = new SelectList(_context.Albums, "ID", "Name", song.AlbumID);
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", song.GenreID);
            return View(song);
        }

        // GET: Songs/Edit/5
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Songs == null)
            {
                return NotFound();
            }

            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }
            ViewData["AlbumID"] = new SelectList(_context.Albums, "ID", "Name", song.AlbumID);
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", song.GenreID);
            return View(song);
        }

        // POST: Songs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            var songToUpdate = await _context.Songs
                .Include(m => m.Performances)
                .Include(m => m.Genre)
                .Include(m => m.Album)
                .FirstOrDefaultAsync(m => m.ID == id);

            _context.Entry(songToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            if (await TryUpdateModelAsync<Song>(songToUpdate, "", s => s.Title, s => s.DateRecorded, s => s.GenreID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Song)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Song was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Song)databaseEntry.ToObject();
                        if (databaseValues.Title != clientValues.Title)
                            ModelState.AddModelError("Name", "Current value: "
                                + databaseValues.Title);
                        if (databaseValues.DateRecorded != clientValues.DateRecorded)
                            ModelState.AddModelError("Name", "Current value: "
                                + databaseValues.DateRecorded);
                        if (databaseValues.GenreID != clientValues.GenreID)
                            ModelState.AddModelError("Name", "Current value: "
                                + databaseValues.GenreID);
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to Song List' hyperlink.");
                        songToUpdate.RowVersion = (byte[])databaseValues.RowVersion;
                        ModelState.Remove("RowVersion");
                    };
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again.");
                }
            }

            //ViewData["AlbumID"] = new SelectList(_context.Albums, "ID", "Name", song.AlbumID);
            //ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", song.GenreID);
            return View(songToUpdate);
        }

        // GET: Songs/Delete/5
        [Authorize(Roles = "Supervisor, Admin")] //* complete for supervisor
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Songs == null)
            {
                return NotFound();
            }

            var song = await _context.Songs
                .Include(s => s.Album)
                .Include(s => s.Genre)
                .FirstOrDefaultAsync(m => m.ID == id);


            if (song == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Supervisor"))
            {
                if (song.CreatedBy != User.Identity.Name)
                {
                    ModelState.AddModelError("", "Supervisors can only delete songs they entered");
                    ViewData["NoSubmit"] = "disabled=disabled";
                }
            }

            return View(song);
        }

        // POST: Songs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor, Admin")] //complete for supervisor
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Songs == null)
            {
                return Problem("Entity set 'MusicContext.Songs'  is null.");
            }
            var song = await _context.Songs.FindAsync(id);

            if (User.IsInRole("Supervisor"))
            {
                if (song.CreatedBy != User.Identity.Name)
                {
                    ModelState.AddModelError("", "Supervisors can only delete songs they entered");
                    ViewData["NoSubmit"] = "disabled=disabled";
                }
            }

            if (song != null)
            {
                _context.Songs.Remove(song);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SongExists(int id)
        {
          return _context.Songs.Any(e => e.ID == id);
        }
    }
}
