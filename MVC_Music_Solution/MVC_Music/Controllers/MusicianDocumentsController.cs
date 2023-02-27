using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Music.Data;
using MVC_Music.Models;
using MVC_Music.Utilities;

namespace MVC_Music.Controllers
{
    [Authorize (Roles = "Admin, Supervisor, Staff")] //view only
    public class MusicianDocumentsController : CustomControllers.ElephantController
    {
        private readonly MusicContext _context;

        public MusicianDocumentsController(MusicContext context)
        {
            _context = context;
        }

        // GET: MusicianDocuments
        [Authorize(Roles = "Staff, Supervisor, Admin")] //need to set up view only for staff
        public async Task<IActionResult> Index(string SearchString, int? MusicianID, string actionButton, int? page, int? pageSizeID)
        {

            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            ViewData["MusicianID"] = new SelectList(_context
                    .Musicians
                    .OrderBy(m => m.LastName), "ID", "FormalName");

            ViewData["Filtering"] = "";

            var musicianDocuments = from m in _context.MusicianDocuments
                .Include(m => m.Musician)
                .OrderBy(m => m.FileName)
                .AsNoTracking()
                            select m;

            if (MusicianID.HasValue)
            {
                musicianDocuments = musicianDocuments.Where(p => p.MusicianID == MusicianID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                musicianDocuments = musicianDocuments.Where(n => n.FileName.ToUpper().Contains(SearchString.ToUpper())
                        || n.MimeType.ToUpper().Contains(SearchString.ToUpper())); //able to search file type, not required but wanted to try it
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(actionButton)) //for paging
            {
                page = 1;
            }

            var musicContext = _context.MusicianDocuments.Include(m => m.Musician);


            //int pageSize = 10;//Change as required
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "musicianDocuments");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<MusicianDocument>.CreateAsync(musicianDocuments, page ?? 1, pageSize);
            //return View(await musicContext.ToListAsync());
            return View(pagedData);
        }

        // GET: MusicianDocuments/Details/5
        [Authorize(Roles = "Supervisor, Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.MusicianDocuments == null)
            {
                return NotFound();
            }

            var musicianDocument = await _context.MusicianDocuments
                .Include(m => m.Musician)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musicianDocument == null)
            {
                return NotFound();
            }

            return View(musicianDocument);
        }

        // GET: MusicianDocuments/Create
        public IActionResult Create()
        {
            ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName");
            return View();
        }

        // POST: MusicianDocuments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MusicianID,ID,FileName,MimeType")] MusicianDocument musicianDocument)
        {
            if (ModelState.IsValid)
            {
                _context.Add(musicianDocument);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName", musicianDocument.MusicianID);
            return View(musicianDocument);
        }

        // GET: MusicianDocuments/Edit/5
        [Authorize(Roles = "Supervisor, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.MusicianDocuments == null)
            {
                return NotFound();
            }

            var musicianDocument = await _context.MusicianDocuments.FindAsync(id);
            if (musicianDocument == null)
            {
                return NotFound();
            }
            ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName", musicianDocument.MusicianID);
            return View(musicianDocument);
        }

        // POST: MusicianDocuments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor, Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("MusicianID,ID,FileName,MimeType")] MusicianDocument musicianDocument)
        {
            if (id != musicianDocument.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(musicianDocument);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MusicianDocumentExists(musicianDocument.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName", musicianDocument.MusicianID);
            return View(musicianDocument);
        }

        // GET: MusicianDocuments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.MusicianDocuments == null)
            {
                return NotFound();
            }

            var musicianDocument = await _context.MusicianDocuments
                .Include(m => m.Musician)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musicianDocument == null)
            {
                return NotFound();
            }

            return View(musicianDocument);
        }

        // POST: MusicianDocuments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.MusicianDocuments == null)
            {
                return Problem("Entity set 'MusicContext.MusicianDocuments'  is null.");
            }
            var musicianDocument = await _context.MusicianDocuments.FindAsync(id);
            if (musicianDocument != null)
            {
                _context.MusicianDocuments.Remove(musicianDocument);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<FileContentResult> Download(int id)
        {
            var theFile = await _context.UploadedFiles
                .Include(m => m.FileContent)
                .Where(f => f.ID == id)
                .FirstOrDefaultAsync();
            return File(theFile.FileContent.Content, theFile.MimeType, theFile.FileName);
        }

        private bool MusicianDocumentExists(int id)
        {
          return _context.MusicianDocuments.Any(e => e.ID == id);
        }
    }
}
