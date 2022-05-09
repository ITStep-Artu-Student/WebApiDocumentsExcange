﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiDocumentsExchange.Data;
using WebApiDocumentsExchange.Extensions;
using WebApiDocumentsExchange.Models;

namespace WebApiDocumentsExchange.Controllers;

[Route("api/[controller]")]
[ApiController]
public class v0Controller : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<v0Controller> _logger;
    public v0Controller(ApplicationDbContext context, ILogger<v0Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Дата і час сервера
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetAsync()
    {
        return Ok(DateTime.Now);
    }
   
    [HttpPost("login")]
    public async Task<IActionResult> PostLoginAsync([FromForm] string nodeName, [FromForm] string password)
    {
        return Ok(await _context.Login(nodeName, password));
    }


    [HttpGet("objects")]
    public async Task<IActionResult> GetObjectsAsync([FromHeader]string apiToken, int take = 10)
    {
        var clientNode = await _context.GetSecureNodeAsync(apiToken);

        var result = await _context.ObjectExchanges.Where(s => s.Destination.Id == clientNode && s.Status == ExchangeStatus.New).Take(take).OrderBy(d=>d.DateStamp).ToArrayAsync();

        return Ok(result);
    }

    [HttpGet("queries")]
    public async Task<IActionResult> GetQueryesAsync(string apiToken, int take = 10)
    {
        var clientNode = await _context.GetSecureNodeAsync(apiToken);

        var result = await _context.QueryObjects
            .Where(s => s.Owner.SenderId == clientNode && s.Status == QueryStatus.New)
            .Take(take)
            .Select(d => new { objectName = d.Owner.ObjectTypeName,
                               Id = d.Owner.Id,
                               QueryJSON=d.QueryJson}).ToArrayAsync();

        return Ok(result);
    }


    [HttpPost]
    public async Task<IActionResult> PostAsync([FromHeader] string apiToken,[FromBody] PostObject postObject)
    {
        var sender = await _context.GetSecureNodeAsync(apiToken);

        foreach (var dest in postObject.DestinationNode)
        {
            // find destination
            var destination = await _context.GetNodeAsync(dest);
            var doc = new ObjectExchange
            {
                Id = new Guid(),
                ObjectId = postObject.ObjectId,
                ObjectTypeName = postObject.ObjectType,
                ObjectJSON = postObject.ObjectJson,
                SenderId = sender,
                DestinationId = destination,
                DateStamp = DateTime.Now,
            };
            await _context.ObjectExchanges.AddAsync(doc);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }


    [HttpPost("status")]
    public async Task<IActionResult> PostMarkResiveObjectsAsync([FromHeader] string apiToken, [FromBody] Guid[] objects)
    {
        var node = await _context.GetSecureNodeAsync(apiToken);

        foreach (var objectId in objects)
        {
            // find destination
            var obj = await _context.ObjectExchanges.FindAsync(objectId);
            if (obj == null)
                throw new Exception($"Not find object with Id - {objectId}");
            if (obj.Status != ExchangeStatus.New && obj.Status != ExchangeStatus.Waited)
                return BadRequest($"Error change recesive status for NodeId={node}; ObjectId={objectId}");
            obj.Status = ExchangeStatus.Resived;
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetRecivedObjectsAsync([FromHeader] string apiToken,int take=10)
    {
        var node = await _context.GetSecureNodeAsync(apiToken);

        var result = await _context.ObjectExchanges
            .Where(f => f.Status == ExchangeStatus.Resived && f.Sender.Id == node).Take(take)
            .Select(s=>new {s.Destination.Name,s.Id })
            .ToArrayAsync();
        return Ok(result);
    }



    [HttpPost("queries")]
    public async Task<IActionResult> PostQueryAsync([FromHeader] string apiToken, [FromBody] QueryObjects queryObjects)
    {
        var sender = await _context.GetSecureNodeAsync(apiToken);
        var obj = await _context.ObjectExchanges.FindAsync(queryObjects.objectId);
        if (obj != null)
        {
            if (obj.DestinationId == sender)
            {
                if (obj.Status == ExchangeStatus.New)
                {
                    obj.Status = ExchangeStatus.Waited;
                    var query = new QueryObject
                    {
                        Owner = obj,
                        Status = QueryStatus.New,
                        QueryJson = queryObjects.ObjectsJson
                    };
                    await _context.QueryObjects.AddAsync(query);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
            }
        }
        throw new Exception();            
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAsync([FromHeader] string apiToken, [FromBody] Guid[] objectsId)
    {
        var sender = await _context.GetSecureNodeAsync(apiToken);
        foreach (var objectId in objectsId)
        {
            // find destination
            var obj = await _context.ObjectExchanges.FindAsync(objectId);
            if (obj == null)
                throw new Exception($"Not find object with Id - {objectId}");
            if (obj.SenderId != sender)
                throw new Exception($"Try delete not owned object with Id -{objectId} ");
            if (obj.Status != ExchangeStatus.Resived)
                throw new Exception($"Try delete not resived object {objectId}");
            _context.ObjectExchanges.Remove(obj);
        }
        await _context.SaveChangesAsync();
        return Ok();
    }
}